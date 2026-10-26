using System.Net;
using System.Text.RegularExpressions;

namespace BigBang1112cz.Tests;

[ClassDataSource<TestWebApplication>(Shared = SharedType.PerClass)]
public partial class PageMetadataTests(TestWebApplication app)
{
    [Test]
    [Arguments("/", "BigBang1112.cz")]
    [Arguments("/projects", "Projects - BigBang1112.cz")]
    [Arguments("/music", "Music - BigBang1112.cz")]
    public async Task Title_HasSiteNameAndPageName(string path, string expectedTitle)
    {
        var html = await app.CreateClient().GetStringAsync(path);

        await Assert.That(Title().Match(html).Groups[1].Value).IsEqualTo(expectedTitle);
    }

    [Test]
    [Arguments("/")]
    [Arguments("/projects")]
    [Arguments("/projects/nativesharp")]
    [Arguments("/links")]
    [Arguments("/music")]
    [Arguments("/blog")]
    [Arguments("/contact")]
    public async Task EveryPage_HasPreviewMetadata(string path)
    {
        var html = await app.CreateClient().GetStringAsync(path);

        await Assert.That(Meta(html, "name", "description")).IsNotEmpty();
        await Assert.That(Meta(html, "property", "og:title")).IsNotEmpty();
        await Assert.That(Meta(html, "property", "og:description")).IsEqualTo(Meta(html, "name", "description"));
        await Assert.That(Meta(html, "property", "og:url")).IsEqualTo($"https://localhost{path}");
        await Assert.That(Meta(html, "property", "og:image")).IsEqualTo("https://localhost/img/og.jpg");
        await Assert.That(Meta(html, "name", "twitter:card")).IsEqualTo("summary_large_image");
        await Assert.That(Meta(html, "name", "theme-color")).IsEqualTo("#222222");
    }

    [Test]
    public async Task PagesWithoutDescription_UseSiteDescription()
    {
        var client = app.CreateClient();

        var home = Meta(await client.GetStringAsync("/"), "name", "description");
        var music = Meta(await client.GetStringAsync("/music"), "name", "description");

        await Assert.That(home).Contains("Realnest");
        await Assert.That(music).IsNotEqualTo(home);
    }

    [Test]
    public async Task PreviewImage_IsServed()
    {
        var response = await app.CreateClient().GetAsync("/img/og.jpg");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/jpeg");
    }

    private static string Meta(string html, string attribute, string name)
    {
        var match = Regex.Match(html, $"<meta {attribute}=\"{Regex.Escape(name)}\" content=\"([^\"]*)\"");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    [GeneratedRegex("<title>([^<]*)</title>")]
    private static partial Regex Title();
}
