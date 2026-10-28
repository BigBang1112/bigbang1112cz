using BigBang1112cz.Models.Blog;
using BigBang1112cz.Services;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace BigBang1112cz.Endpoints;

/// <summary>
/// RSS 2.0 feed of published blog posts with full content, at /blog/feed.xml.
/// </summary>
public static partial class BlogFeedEndpoint
{
    public const string Path = "/blog/feed.xml";

    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace Content = "http://purl.org/rss/1.0/modules/content/";

    // RSS's own <author> has to be an email address, so feeds use Dublin Core's creator for names
    private static readonly XNamespace DublinCore = "http://purl.org/dc/elements/1.1/";

    public static void MapBlogFeed(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, (HttpContext context, BlogCatalog catalog, TimeProvider timeProvider) =>
        {
            // Scheduled posts never go into the feed, not even in development, as readers would keep them
            var posts = catalog.GetVisible(timeProvider.GetUtcNow(), includeScheduled: false).ToList();

            // Feeds need absolute URLs, https like the link previews in _Layout
            var feed = Build($"https://{context.Request.Host}", posts);

            return Results.Bytes(feed, "application/rss+xml; charset=utf-8");
        })
        .CacheOutput(policy => policy.Expire(TimeSpan.FromHours(1)).Tag(BlogCatalog.CacheTag));
    }

    internal static byte[] Build(string siteUrl, IReadOnlyList<BlogPost> posts)
    {
        var channel = new XElement("channel",
            new XElement("title", "BigBang1112.cz blog"),
            new XElement("link", $"{siteUrl}/blog"),
            new XElement("description", BlogCatalog.Description),
            new XElement("language", "en"),
            new XElement(Atom + "link",
                new XAttribute("href", siteUrl + Path),
                new XAttribute("rel", "self"),
                new XAttribute("type", "application/rss+xml")));

        if (posts.Count > 0)
        {
            channel.Add(new XElement("lastBuildDate", FormatDate(posts.Max(x => x.Published))));
        }

        foreach (var post in posts)
        {
            var url = $"{siteUrl}/blog/{post.Slug}";

            channel.Add(new XElement("item",
                new XElement("title", post.Title),
                new XElement("link", url),
                new XElement("guid", new XAttribute("isPermaLink", "true"), url),
                new XElement(DublinCore + "creator", post.Author),
                new XElement("pubDate", FormatDate(post.Published)),
                new XElement("description", post.Description),
                new XElement(Content + "encoded", new XCData(
                    MakeUrlsAbsolute(post.ContentHtml, siteUrl) + $"<p><em>{BlogCatalog.AiNote}</em></p>"))));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XAttribute(XNamespace.Xmlns + "atom", Atom),
                new XAttribute(XNamespace.Xmlns + "content", Content),
                new XAttribute(XNamespace.Xmlns + "dc", DublinCore),
                channel));

        using var stream = new MemoryStream();

        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }

    // RFC 822 dates as RSS requires
    private static string FormatDate(DateTimeOffset date)
    {
        return date.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
    }

    // Readers resolve relative links inconsistently, so site-relative ones like /img/... get the site URL
    private static string MakeUrlsAbsolute(string html, string siteUrl)
    {
        return SiteRelativeUrlRegex().Replace(html, $"$1=\"{siteUrl}/");
    }

    [GeneratedRegex("\\b(href|src)=\"/(?!/)")]
    private static partial Regex SiteRelativeUrlRegex();
}
