using BigBang1112cz.Models.Music;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace BigBang1112cz.Tests;

public class MusicCatalogParseTests
{
    [Test]
    public async Task Parse_ReadsFrontMatterAndMarkdown()
    {
        var release = MusicCatalog.Parse("test.md", """
            ---
            title: "Song: The Title"
            released: 2024-08-13
            spotify: album/7tBEvDD1Sehxc31IdETQjr
            ---

            Some **bold** text.
            """);

        await Assert.That(release.Title).IsEqualTo("Song: The Title");
        await Assert.That(release.Released).IsEqualTo(new DateOnly(2024, 8, 13));
        await Assert.That(release.Type).IsEqualTo(MusicReleaseType.Original);
        await Assert.That(release.ContentHtml).Contains("<p>Some <strong>bold</strong> text.</p>");
        await Assert.That(release.ContentHtml).DoesNotContain("released");
        await Assert.That(release.Embed).IsEqualTo(new MusicEmbed(MusicPlatform.Spotify, "https://open.spotify.com/embed/album/7tBEvDD1Sehxc31IdETQjr?utm_source=generator", 352));
    }

    [Test]
    [Arguments("track/4gjVtmWTxE2KFL1jaeAuIs")]
    [Arguments("https://open.spotify.com/track/4gjVtmWTxE2KFL1jaeAuIs")]
    [Arguments("https://open.spotify.com/intl-de/track/4gjVtmWTxE2KFL1jaeAuIs?si=abc123")]
    public async Task Parse_AcceptsSpotifyShortFormAndShareLinks(string spotify)
    {
        var release = MusicCatalog.Parse("test.md", $"""
            ---
            title: Far Away VIP
            released: 2021-12-24
            spotify: {spotify}
            ---
            """);

        await Assert.That(release.Embed).IsEqualTo(new MusicEmbed(MusicPlatform.Spotify, "https://open.spotify.com/embed/track/4gjVtmWTxE2KFL1jaeAuIs?utm_source=generator", 152));
    }

    [Test]
    public async Task Parse_BuildsSoundCloudPlayerUrl()
    {
        var release = MusicCatalog.Parse("test.md", """
            ---
            title: ManiaPlanet Stories
            released: 2023-11-10
            type: remix
            soundcloud: playlists/1719132375
            ---
            """);

        await Assert.That(release.Type).IsEqualTo(MusicReleaseType.Remix);
        await Assert.That(release.Embed!.Source).IsEqualTo("https://w.soundcloud.com/player/?url=https%3A//api.soundcloud.com/playlists/soundcloud%253Aplaylists%253A1719132375&color=%23ff5500&auto_play=false&hide_related=false&show_comments=true&show_user=true&show_reposts=false&show_teaser=true&visual=true");
        await Assert.That(release.Embed.Height).IsEqualTo(300);
    }

    [Test]
    [Arguments("No front matter at all.", "missing front matter")]
    [Arguments("---\nreleased: 2024-01-01\n---", "'title' is required")]
    [Arguments("---\ntitle: X\nreleased: 13. 8. 2024\n---", "'released' is required")]
    [Arguments("---\ntitle: X\nrelased: 2024-01-01\n---", "invalid front matter")]
    [Arguments("---\ntitle: X\nreleased: 2024-01-01\ntype: album\n---", "'type' has to be")]
    [Arguments("---\ntitle: X\nreleased: 2024-01-01\nspotify: album/a\nsoundcloud: tracks/1\n---", "not both")]
    [Arguments("---\ntitle: X\nreleased: 2024-01-01\nspotify: https://example.com/album/a\n---", "'spotify' has to be")]
    [Arguments("---\ntitle: X\nreleased: 2024-01-01\nsoundcloud: https://soundcloud.com/realnest/x\n---", "'soundcloud' has to be")]
    public async Task Parse_InvalidFile_ExplainsTheProblem(string markdown, string expectedMessage)
    {
        var exception = await Assert.That(() => { MusicCatalog.Parse("broken.md", markdown); }).Throws<InvalidDataException>();

        await Assert.That(exception!.Message).StartsWith("broken.md:");
        await Assert.That(exception.Message).Contains(expectedMessage);
    }
}

[ClassDataSource<TestWebApplication>(Shared = SharedType.PerClass)]
public partial class MusicPageTests(TestWebApplication app)
{
    [Test]
    public async Task Template_IsValid()
    {
        var path = Path.Combine(ContentDirectory(), "_template.md");

        var release = MusicCatalog.Parse("_template.md", await File.ReadAllTextAsync(path));

        await Assert.That(release.Embed).IsNotNull();
    }

    [Test]
    public async Task Page_ShowsEveryReleaseNewestFirst()
    {
        var expectedCount = Directory.GetFiles(ContentDirectory(), "*.md").Count(x => !Path.GetFileName(x).StartsWith('_'));

        var html = await app.CreateClient().GetStringAsync("/music");
        var dates = Released().Matches(html).Select(x => DateOnly.Parse(x.Groups[1].Value)).ToList();

        await Assert.That(dates).Count().IsEqualTo(expectedCount);
        await Assert.That(dates).IsOrderedByDescending(x => x);
        await Assert.That(html).DoesNotContain("_template");
    }

    [Test]
    public async Task Page_PlayersAreLazyAndTitled()
    {
        var html = await app.CreateClient().GetStringAsync("/music");
        var iframes = Iframe().Matches(html).Select(x => x.Value).ToList();

        await Assert.That(iframes).IsNotEmpty();

        foreach (var iframe in iframes)
        {
            await Assert.That(iframe).Contains("loading=\"lazy\"");
            await Assert.That(iframe).Matches(" title=\"[^\"]+ on (Spotify|SoundCloud)\"");
        }
    }

    private string ContentDirectory()
    {
        var env = app.Services.GetRequiredService<IWebHostEnvironment>();
        return Path.Combine(env.ContentRootPath, MusicCatalog.DirectoryPath);
    }

    [GeneratedRegex("<time [^>]*datetime=\"([0-9-]+)\"")]
    private static partial Regex Released();

    [GeneratedRegex("<iframe [^>]*>")]
    private static partial Regex Iframe();
}
