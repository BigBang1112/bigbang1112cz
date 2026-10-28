using BigBang1112cz.Models.Blog;
using Markdig;
using Markdig.Syntax;
using Microsoft.AspNetCore.OutputCaching;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace BigBang1112cz.Services;

/// <summary>
/// Blog posts written as Markdown files with YAML front matter in <see cref="DirectoryPath"/>.
/// The URL slug is the file name without the date prefix, e.g. 2026-10-30-gbxnet-2.5.0.md is /blog/gbxnet-2.5.0.
/// </summary>
public sealed partial class BlogCatalog : MarkdownCatalog<BlogPost>
{
    public const string DirectoryPath = "Content/Blog";
    public const string CacheTag = "blog";

    // Shared by the blog pages and the RSS feed
    public const string Description = "Posts by BigBang1112 about projects, Trackmania discoveries, and more.";
    public const string AiNote = "These blog posts are NOT written with AI. LLMs are only used for numeric results, benchmarks, knowledge from the decompiled game code, and site construction.";

    public const string DefaultAuthor = "Petr Pivoňka";

    private const int WordsPerMinute = 200;
    private const int MaxDescriptionLength = 200;

    // Timers can't wait longer than ~49 days, so waits for posts scheduled further ahead are split up
    private static readonly TimeSpan MaxTimerDelay = TimeSpan.FromDays(1);

    private readonly TimeProvider timeProvider;
    private readonly Lock timerLock = new();
    private ITimer? publishTimer;

    public IReadOnlyList<BlogPost> Posts => Items;

    public BlogCatalog(IWebHostEnvironment env, IOutputCacheStore cache, TimeProvider timeProvider, ILogger<BlogCatalog> logger)
        : base(DirectoryPath, CacheTag, env, cache, logger)
    {
        this.timeProvider = timeProvider;
        SchedulePublishing();
    }

    /// <summary>
    /// Posts published after <paramref name="now"/> are scheduled and hidden, unless <paramref name="includeScheduled"/> is set to preview them.
    /// </summary>
    public IEnumerable<BlogPost> GetVisible(DateTimeOffset now, bool includeScheduled) => Visible(Posts, now, includeScheduled);

    public BlogPost? Find(string slug, DateTimeOffset now, bool includeScheduled)
    {
        return GetVisible(now, includeScheduled).FirstOrDefault(x => x.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    internal static IEnumerable<BlogPost> Visible(IEnumerable<BlogPost> posts, DateTimeOffset now, bool includeScheduled)
    {
        return includeScheduled ? posts : posts.Where(x => x.Published <= now);
    }

    protected override void OnItemsReloaded() => SchedulePublishing();

    /// <summary>
    /// The blog pages and the feed are output-cached for an hour, so the cache is cleared right when the next scheduled post goes live.
    /// </summary>
    private void SchedulePublishing()
    {
        lock (timerLock)
        {
            publishTimer?.Dispose();
            publishTimer = null;

            var now = timeProvider.GetUtcNow();
            var next = Posts.Where(x => x.Published > now).Select(x => (DateTimeOffset?)x.Published).Min();

            if (next is null)
            {
                return;
            }

            var delay = next.Value - now;
            publishTimer = timeProvider.CreateTimer(_ =>
            {
                EvictCache();
                SchedulePublishing();
            }, null, delay < MaxTimerDelay ? delay : MaxTimerDelay, Timeout.InfiniteTimeSpan);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (timerLock)
            {
                publishTimer?.Dispose();
                publishTimer = null;
            }
        }

        base.Dispose(disposing);
    }

    protected override BlogPost ParseFile(string fileName, string markdown) => Parse(fileName, markdown);

    protected override IEnumerable<BlogPost> Arrange(IEnumerable<BlogPost> items)
    {
        var posts = items.ToList();

        if (posts.GroupBy(x => x.Slug).FirstOrDefault(x => x.Count() > 1) is { } duplicate)
        {
            throw new InvalidDataException($"More than one blog post uses the URL /blog/{duplicate.Key}, rename one of the files.");
        }

        return posts.OrderByDescending(x => x.Published).ThenBy(x => x.Title);
    }

    internal static BlogPost Parse(string fileName, string markdown)
    {
        var slugMatch = FileNameRegex().Match(fileName);

        if (!slugMatch.Success)
        {
            throw new InvalidDataException($"{fileName}: the file name becomes the URL, so use only lowercase letters, numbers, dots and dashes, like 2026-10-30-some-title.md.");
        }

        var (frontMatter, document) = ParseMarkdown<FrontMatter>(fileName, markdown);

        var title = Required(fileName, "title", frontMatter.Title);
        var published = ParsePublished(fileName, frontMatter.Published);
        var author = string.IsNullOrWhiteSpace(frontMatter.Author) ? DefaultAuthor : frontMatter.Author.Trim();
        var html = ToHtml(document);

        var description = string.IsNullOrWhiteSpace(frontMatter.Description)
            ? Summarize(markdown, document)
            : frontMatter.Description.Trim();

        var ogImage = string.IsNullOrWhiteSpace(frontMatter.OgImage) ? null : frontMatter.OgImage.Trim();

        return new BlogPost(slugMatch.Groups["slug"].Value, title, author, published, description, ReadingMinutes(html), html, ogImage);
    }

    /// <summary>
    /// A date, or a date with time for publishing at an exact minute, both in UTC.
    /// </summary>
    private static DateTimeOffset ParsePublished(string fileName, string? value)
    {
        return DateTime.TryParseExact(value, ["yyyy-MM-dd HH:mm", "yyyy-MM-dd"], CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var published)
            ? new DateTimeOffset(published, TimeSpan.Zero)
            : throw new InvalidDataException($"{fileName}: 'published' is required as yyyy-MM-dd or yyyy-MM-dd HH:mm, in UTC.");
    }

    /// <summary>
    /// The first paragraph as plain text, shortened at a word boundary.
    /// </summary>
    private static string Summarize(string markdown, MarkdownDocument document)
    {
        if (document.Descendants<ParagraphBlock>().FirstOrDefault() is not { } paragraph)
        {
            return "";
        }

        var text = WhitespaceRegex().Replace(Markdig.Markdown.ToPlainText(markdown.Substring(paragraph.Span.Start, paragraph.Span.Length)), " ").Trim();

        if (text.Length <= MaxDescriptionLength)
        {
            return text;
        }

        var cut = text.LastIndexOf(' ', MaxDescriptionLength);
        return text[..(cut > 0 ? cut : MaxDescriptionLength)].TrimEnd(',', '.', ';', ':') + "…";
    }

    private static int ReadingMinutes(string html)
    {
        var text = WebUtility.HtmlDecode(TagRegex().Replace(html, " "));
        var words = WordRegex().Count(text);
        return Math.Max(1, (int)Math.Round(words / (double)WordsPerMinute));
    }

    [GeneratedRegex(@"^(?:\d{4}-\d{2}-\d{2}-)?(?<slug>[a-z0-9][a-z0-9.-]*)\.md$")]
    private static partial Regex FileNameRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’.-][\p{L}\p{N}]+)*")]
    private static partial Regex WordRegex();

    private sealed class FrontMatter
    {
        public string? Title { get; set; }
        public string? Published { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? OgImage { get; set; }
    }
}
