using BigBang1112cz.Models.Blog;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.RegularExpressions;

namespace BigBang1112cz.Tests;

public class BlogParseTests
{
    [Test]
    public async Task Parse_TakesSlugFromFileNameWithoutDate()
    {
        var post = ParsePost("2026-10-30-gbxnet-2.5.0.md", """
            ---
            title: "GBX.NET 2.5.0: what's new"
            published: 2026-10-30
            ---

            First paragraph.
            """);

        await Assert.That(post.Slug).IsEqualTo("gbxnet-2.5.0");
        await Assert.That(post.Title).IsEqualTo("GBX.NET 2.5.0: what's new");
        await Assert.That(post.Author).IsEqualTo("Petr Pivoňka");
        await Assert.That(post.Published).IsEqualTo(new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero));
        await Assert.That(post.PublishedDate).IsEqualTo(new DateOnly(2026, 10, 30));
    }

    [Test]
    public async Task Parse_PublishedTimeIsUtcWithMinutePrecision()
    {
        var post = ParsePost("post.md", """
            ---
            title: Post
            published: 2026-10-30 23:45
            author: Someone Else
            ---
            """);

        await Assert.That(post.Published).IsEqualTo(new DateTimeOffset(2026, 10, 30, 23, 45, 0, TimeSpan.Zero));
        await Assert.That(post.PublishedDate).IsEqualTo(new DateOnly(2026, 10, 30));
        await Assert.That(post.Author).IsEqualTo("Someone Else");
    }

    [Test]
    public async Task Parse_DescriptionDefaultsToFirstParagraphAsPlainText()
    {
        var post = ParsePost("post.md", """
            ---
            title: Post
            published: 2026-01-01
            ---

            Some **bold** and [linked](https://example.com) text
            on two lines.

            ## Heading

            Second paragraph.
            """);

        await Assert.That(post.Description).IsEqualTo("Some bold and linked text on two lines.");
    }

    [Test]
    public async Task Parse_LongDescriptionIsShortenedAtWordBoundary()
    {
        var words = string.Join(' ', Enumerable.Repeat("word", 100));

        var post = ParsePost("post.md", $"""
            ---
            title: Post
            published: 2026-01-01
            ---

            {words}
            """);

        await Assert.That(post.Description.Length).IsLessThanOrEqualTo(201);
        await Assert.That(post.Description).EndsWith("word…");
    }

    [Test]
    public async Task Parse_ExplicitDescriptionWins()
    {
        var post = ParsePost("post.md", """
            ---
            title: Post
            published: 2026-01-01
            description: Written by hand.
            ---

            First paragraph.
            """);

        await Assert.That(post.Description).IsEqualTo("Written by hand.");
    }

    [Test]
    public async Task Parse_ReadingTimeIsAtLeastOneMinute()
    {
        var longText = string.Join(' ', Enumerable.Repeat("word", 1000));

        var shortPost = ParsePost("short.md", "---\ntitle: Short\npublished: 2026-01-01\n---\n\nHi.");
        var longPost = ParsePost("long.md", $"---\ntitle: Long\npublished: 2026-01-01\n---\n\n{longText}");

        await Assert.That(shortPost.ReadingMinutes).IsEqualTo(1);
        await Assert.That(longPost.ReadingMinutes).IsEqualTo(5);
    }

    [Test]
    [Arguments("Post.md", "---\ntitle: X\npublished: 2026-01-01\n---", "the file name becomes the URL")]
    [Arguments("my post.md", "---\ntitle: X\npublished: 2026-01-01\n---", "the file name becomes the URL")]
    [Arguments("post.md", "---\npublished: 2026-01-01\n---", "'title' is required")]
    [Arguments("post.md", "---\ntitle: X\n---", "'published' is required")]
    [Arguments("post.md", "---\ntitle: X\npublished: 30. 10. 2026\n---", "'published' is required")]
    [Arguments("post.md", "---\ntitle: X\npublished: 2026-10-30 18:30:15\n---", "'published' is required")]
    [Arguments("post.md", "---\ntitle: X\npublished: 2026-01-01\ntags: me\n---", "invalid front matter")]
    [Arguments("post.md", "No front matter.", "missing front matter")]
    public async Task Parse_InvalidFile_ExplainsTheProblem(string fileName, string markdown, string expectedMessage)
    {
        var exception = await Assert.That(() => { BlogCatalog.Parse(fileName, markdown); }).Throws<InvalidDataException>();

        await Assert.That(exception!.Message).Contains(expectedMessage);
    }

    [Test]
    public async Task Visible_HidesScheduledPostsUnlessPreviewing()
    {
        var now = new DateTimeOffset(2026, 10, 30, 18, 30, 0, TimeSpan.Zero);
        BlogPost[] posts =
        [
            new("next-minute", "Next minute", "BigBang1112", now.AddMinutes(1), "", 1, ""),
            new("this-minute", "This minute", "BigBang1112", now, "", 1, ""),
        ];

        await Assert.That(BlogCatalog.Visible(posts, now, includeScheduled: false).Select(x => x.Slug)).IsEquivalentTo(["this-minute"]);
        await Assert.That(BlogCatalog.Visible(posts, now, includeScheduled: true).Select(x => x.Slug)).IsEquivalentTo(["next-minute", "this-minute"]);
    }

    [Test]
    public async Task Parse_HighlightsCSharpCodeBlocks()
    {
        var post = ParsePost("post.md", """
            ---
            title: Post
            published: 2026-01-01
            ---

            ```cs
            // Create a clip
            var name = "Intro";
            ```

            ```
            var plain = "not highlighted";
            ```
            """);

        await Assert.That(post.ContentHtml).Contains("<span class=\"comment\">// Create a clip</span>");
        await Assert.That(post.ContentHtml).Contains("<span class=\"keyword\">var</span> name");
        await Assert.That(post.ContentHtml).Contains("<span class=\"string\">&quot;Intro&quot;</span>");
        await Assert.That(post.ContentHtml).Contains("<pre><code>var plain = &quot;not highlighted&quot;;");
    }

    private static BlogPost ParsePost(string fileName, string markdown) => BlogCatalog.Parse(fileName, markdown);
}

// The tests move the shared clock, so they must not overlap
[ClassDataSource<FixedTimeWebApplication>(Shared = SharedType.PerClass)]
[NotInParallel]
public partial class BlogPageTests(FixedTimeWebApplication app)
{
    [Test]
    public async Task Template_IsValid()
    {
        var path = Path.Combine(ContentDirectory(), "_template.md");

        var post = BlogCatalog.Parse("2026-01-31-template.md", await File.ReadAllTextAsync(path));

        await Assert.That(post.Description).IsNotEmpty();
    }

    [Test]
    public async Task Index_ListsEveryPostWithLinks()
    {
        var expectedCount = Directory.GetFiles(ContentDirectory(), "*.md").Count(x => !Path.GetFileName(x).StartsWith('_'));

        var html = await GetAtAsync("/blog", new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var links = PostLink().Matches(html).Select(x => x.Groups[1].Value).ToList();

        await Assert.That(links).Count().IsEqualTo(expectedCount);
        await Assert.That(html).DoesNotContain("Scheduled");
    }

    [Test]
    public async Task Post_RendersTitleContentAndPreviewDescription()
    {
        var post = Catalog().Posts[0];

        var html = await GetAtAsync($"/blog/{post.Slug}", post.Published);

        // Razor and WebUtility encode characters like · differently, so compare decoded text
        var heading = WebUtility.HtmlDecode(Regex.Match(html, "<h1[^>]*>(.*?)</h1>").Groups[1].Value);
        var description = WebUtility.HtmlDecode(Regex.Match(html, "<meta name=\"description\" content=\"([^\"]*)\"").Groups[1].Value);

        await Assert.That(heading).IsEqualTo(post.Title);
        await Assert.That(description).IsEqualTo(post.Description);
        await Assert.That(html).Contains("class=\"post-content\"");
        await Assert.That(html).Contains("These blog posts are NOT written with AI.");
        await Assert.That(html).DoesNotContain("Scheduled");
    }

    [Test]
    public async Task Post_UsesItsOwnSharingImageWithoutChangingOtherPages()
    {
        var post = Catalog().Posts.Single(x => x.Slug == "over-4500-member-names-discovered-gbxnet-2.5.0");

        var html = await GetAtAsync($"/blog/{post.Slug}", post.Published);
        var index = await GetAtAsync("/blog", post.Published);

        await Assert.That(html).Contains("<meta property=\"og:image\" content=\"https://localhost/img/blog1.jpg\" />");
        await Assert.That(html).DoesNotContain("og:image:width");
        await Assert.That(html).DoesNotContain("og:image:height");
        await Assert.That(html).DoesNotContain("<img");
        await Assert.That(index).Contains("<meta property=\"og:image\" content=\"https://localhost/img/og.jpg\" />");
        await Assert.That(index).Contains("<meta property=\"og:image:width\" content=\"1205\" />");
        await Assert.That(index).Contains("<meta property=\"og:image:height\" content=\"352\" />");
    }

    [Test]
    public async Task Post_ShowsAuthorAndOnlyTheDate()
    {
        var post = Catalog().Posts[0];

        var html = await GetAtAsync($"/blog/{post.Slug}", post.Published);
        var time = Regex.Match(html, "<time [^>]*datetime=\"([^\"]+)\"[^>]*>([^<]+)</time>");

        // Razor encodes characters like ň, so compare decoded text
        var author = WebUtility.HtmlDecode(Regex.Match(html, "class=\"post-author\"[^>]*>([^<]*)</span>").Groups[1].Value);

        await Assert.That(author).IsEqualTo($"by {post.Author}");
        await Assert.That(time.Groups[2].Value).IsEqualTo(post.PublishedDate.ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture));
        await Assert.That(DateTimeOffset.Parse(time.Groups[1].Value)).IsEqualTo(post.Published);
    }

    [Test]
    public async Task ScheduledPost_IsMarkedInDevelopment()
    {
        var post = Catalog().Posts[0];
        var minuteBefore = post.Published.AddMinutes(-1);

        var index = await GetAtAsync("/blog", minuteBefore);
        var page = await GetAtAsync($"/blog/{post.Slug}", minuteBefore);

        await Assert.That(index).Contains("Scheduled");
        await Assert.That(page).Contains("Scheduled, only visible in development");
    }

    [Test]
    public async Task UnknownPost_IsNotFound()
    {
        var response = await app.CreateClient().GetAsync("/blog/this-post-does-not-exist");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private BlogCatalog Catalog() => app.Services.GetRequiredService<BlogCatalog>();

    private async Task<string> GetAtAsync(string path, DateTimeOffset now)
    {
        app.Time.UtcNow = now;
        await app.EvictCacheAsync(BlogCatalog.CacheTag);

        return await app.CreateClient().GetStringAsync(path);
    }

    private string ContentDirectory()
    {
        var env = app.Services.GetRequiredService<IWebHostEnvironment>();
        return Path.Combine(env.ContentRootPath, BlogCatalog.DirectoryPath);
    }

    [GeneratedRegex("<a [^>]*class=\"card\"[^>]*href=\"/blog/([^\"]+)\"|<a [^>]*href=\"/blog/([^\"]+)\"[^>]*class=\"card\"")]
    private static partial Regex PostLink();
}
