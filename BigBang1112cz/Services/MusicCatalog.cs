using BigBang1112cz.Models.Music;
using Markdig;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.RegularExpressions;

namespace BigBang1112cz.Services;

/// <summary>
/// Music releases written as Markdown files with YAML front matter in <see cref="DirectoryPath"/>.
/// </summary>
public sealed partial class MusicCatalog : MarkdownCatalog<MusicRelease>
{
    public const string DirectoryPath = "Content/Music";
    public const string CacheTag = "music";

    public IReadOnlyList<MusicRelease> Releases => Items;

    public MusicCatalog(IWebHostEnvironment env, IOutputCacheStore cache, ILogger<MusicCatalog> logger)
        : base(DirectoryPath, CacheTag, env, cache, logger)
    {
    }

    protected override MusicRelease ParseFile(string fileName, string markdown) => Parse(fileName, markdown);

    protected override IEnumerable<MusicRelease> Arrange(IEnumerable<MusicRelease> items)
    {
        return items.OrderByDescending(x => x.Released).ThenBy(x => x.Title);
    }

    internal static MusicRelease Parse(string fileName, string markdown)
    {
        var (frontMatter, document) = ParseMarkdown<FrontMatter>(fileName, markdown);

        var title = Required(fileName, "title", frontMatter.Title);
        var released = RequiredDate(fileName, "released", frontMatter.Released);

        var type = frontMatter.Type switch
        {
            null or "original" => MusicReleaseType.Original,
            "remix" => MusicReleaseType.Remix,
            _ => throw new InvalidDataException($"{fileName}: 'type' has to be 'original' or 'remix'.")
        };

        return new MusicRelease(title, released, type, ParseEmbed(fileName, frontMatter), document.ToHtml(Pipeline));
    }

    private static MusicEmbed? ParseEmbed(string fileName, FrontMatter frontMatter)
    {
        if (frontMatter.Spotify is not null && frontMatter.Soundcloud is not null)
        {
            throw new InvalidDataException($"{fileName}: use either 'spotify' or 'soundcloud', not both.");
        }

        if (frontMatter.Spotify is not null)
        {
            var match = SpotifyRegex().Match(frontMatter.Spotify);

            if (!match.Success)
            {
                throw new InvalidDataException($"{fileName}: 'spotify' has to be album/<id>, track/<id> or playlist/<id>, or an open.spotify.com link.");
            }

            var kind = match.Groups["kind"].Value;
            var id = match.Groups["id"].Value;

            return new MusicEmbed(
                MusicPlatform.Spotify,
                $"https://open.spotify.com/embed/{kind}/{id}?utm_source=generator",
                Height: kind == "track" ? 152 : 352);
        }

        if (frontMatter.Soundcloud is not null)
        {
            var match = SoundCloudRegex().Match(frontMatter.Soundcloud);

            if (!match.Success)
            {
                throw new InvalidDataException($"{fileName}: 'soundcloud' has to be tracks/<id> or playlists/<id>, with the number from the SoundCloud embed code.");
            }

            var kind = match.Groups["kind"].Value;
            var id = match.Groups["id"].Value;

            return new MusicEmbed(
                MusicPlatform.SoundCloud,
                $"https://w.soundcloud.com/player/?url=https%3A//api.soundcloud.com/{kind}/soundcloud%253A{kind}%253A{id}&color=%23ff5500&auto_play=false&hide_related=false&show_comments=true&show_user=true&show_reposts=false&show_teaser=true&visual=true",
                Height: 300);
        }

        return null;
    }

    // Accepts the short form and share links, including localized ones like open.spotify.com/intl-de/album/...
    [GeneratedRegex(@"^(?:https://open\.spotify\.com/(?:intl-[a-z-]+/)?)?(?<kind>album|track|playlist)/(?<id>[A-Za-z0-9]+)(?:\?.*)?$")]
    private static partial Regex SpotifyRegex();

    [GeneratedRegex(@"^(?<kind>tracks|playlists)/(?<id>[0-9]+)$")]
    private static partial Regex SoundCloudRegex();

    private sealed class FrontMatter
    {
        public string? Title { get; set; }
        public string? Released { get; set; }
        public string? Type { get; set; }
        public string? Spotify { get; set; }
        public string? Soundcloud { get; set; }
    }
}
