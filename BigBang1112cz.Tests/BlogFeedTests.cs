using BigBang1112cz.Endpoints;
using BigBang1112cz.Models.Blog;
using BigBang1112cz.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace BigBang1112cz.Tests;

public class BlogFeedBuildTests
{
    private static readonly XNamespace Content = "http://purl.org/rss/1.0/modules/content/";

    [Test]
    public async Task Build_MakesSiteRelativeUrlsAbsolute()
    {
        var post = new BlogPost("post", "Post", "BigBang1112", new DateTimeOffset(2026, 10, 30, 18, 30, 0, TimeSpan.Zero), "Summary", 1,
            """<p><a href="/projects">Projects</a> <img src="/img/og.jpg"> <a href="https://github.com">GitHub</a> <a href="//cdn.example.com/x">CDN</a> <a href="#builders">Anchor</a></p>""");

        var feed = XDocument.Parse(Encoding.UTF8.GetString(BlogFeedEndpoint.Build("https://bigbang1112.cz", [post])));
        var content = feed.Descendants(Content + "encoded").Single().Value;

        await Assert.That(content).Contains("href=\"https://bigbang1112.cz/projects\"");
        await Assert.That(content).Contains("src=\"https://bigbang1112.cz/img/og.jpg\"");
        await Assert.That(content).Contains("href=\"https://github.com\"");
        await Assert.That(content).Contains("href=\"//cdn.example.com/x\"");
        await Assert.That(content).Contains("href=\"#builders\"");
        await Assert.That(content).EndsWith($"<p><em>{BlogCatalog.AiNote}</em></p>");
    }

    [Test]
    public async Task Build_EmptyFeedIsStillValid()
    {
        var feed = XDocument.Parse(Encoding.UTF8.GetString(BlogFeedEndpoint.Build("https://bigbang1112.cz", [])));

        await Assert.That(feed.Root!.Name.LocalName).IsEqualTo("rss");
        await Assert.That(feed.Descendants("item")).IsEmpty();
        await Assert.That(feed.Descendants("lastBuildDate")).IsEmpty();
    }
}

// The tests move the shared clock, so they must not overlap
[ClassDataSource<FixedTimeWebApplication>(Shared = SharedType.PerClass)]
[NotInParallel]
public class BlogFeedTests(FixedTimeWebApplication app)
{
    [Test]
    public async Task Feed_IsRssWithEveryPublishedPost()
    {
        var posts = app.Services.GetRequiredService<BlogCatalog>().Posts;

        var (response, feed) = await GetFeedAtAsync(new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/rss+xml");
        await Assert.That(feed.Root!.Attribute("version")?.Value).IsEqualTo("2.0");

        var items = feed.Descendants("item").ToList();
        await Assert.That(items).Count().IsEqualTo(posts.Count);

        foreach (var (item, post) in items.Zip(posts))
        {
            var url = $"https://localhost/blog/{post.Slug}";

            await Assert.That(item.Element("title")?.Value).IsEqualTo(post.Title);
            await Assert.That(item.Element("link")?.Value).IsEqualTo(url);
            await Assert.That(item.Element("guid")?.Value).IsEqualTo(url);
            await Assert.That(item.Element("description")?.Value).IsEqualTo(post.Description);
            await Assert.That(item.Element(DublinCore + "creator")?.Value).IsEqualTo(post.Author);
            await Assert.That(DateTimeOffset.ParseExact(item.Element("pubDate")!.Value, "R", CultureInfo.InvariantCulture)).IsEqualTo(post.Published);
        }
    }

    [Test]
    public async Task Feed_LeavesOutScheduledPosts()
    {
        var newest = app.Services.GetRequiredService<BlogCatalog>().Posts[0];

        var (_, minuteBefore) = await GetFeedAtAsync(newest.Published.AddMinutes(-1));
        var (_, onTheMinute) = await GetFeedAtAsync(newest.Published);

        await Assert.That(minuteBefore.Descendants("guid").Select(x => x.Value)).DoesNotContain($"https://localhost/blog/{newest.Slug}");
        await Assert.That(onTheMinute.Descendants("guid").Select(x => x.Value)).Contains($"https://localhost/blog/{newest.Slug}");
    }

    [Test]
    public async Task Pages_AdvertiseTheFeed()
    {
        var html = await app.CreateClient().GetStringAsync("/");

        await Assert.That(html).Contains("<link rel=\"alternate\" type=\"application/rss+xml\" title=\"BigBang1112.cz blog\" href=\"/blog/feed.xml\" />");
    }

    private static readonly XNamespace DublinCore = "http://purl.org/dc/elements/1.1/";

    private async Task<(HttpResponseMessage Response, XDocument Feed)> GetFeedAtAsync(DateTimeOffset now)
    {
        app.Time.UtcNow = now;
        await app.EvictCacheAsync(BlogCatalog.CacheTag);

        var response = await app.CreateClient().GetAsync(BlogFeedEndpoint.Path);
        response.EnsureSuccessStatusCode();

        return (response, XDocument.Parse(await response.Content.ReadAsStringAsync()));
    }
}
