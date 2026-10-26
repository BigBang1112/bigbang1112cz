using BigBang1112cz.Models.Music;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BigBang1112cz.Services;

/// <summary>
/// Music releases written as Markdown files with YAML front matter in <see cref="DirectoryPath"/>.
/// Files starting with an underscore are ignored. In development, the files are reloaded when they change.
/// </summary>
public sealed partial class MusicCatalog : IDisposable
{
    public const string DirectoryPath = "Content/Music";
    public const string CacheTag = "music";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UseAdvancedExtensions()
        .Build();

    // Unknown front matter keys throw, so typos don't go unnoticed
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .Build();

    private readonly IFileProvider files;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<MusicCatalog> logger;
    private readonly IDisposable? watcher;

    public IReadOnlyList<MusicRelease> Releases { get; private set; }

    public MusicCatalog(IWebHostEnvironment env, IOutputCacheStore cache, ILogger<MusicCatalog> logger)
    {
        files = env.ContentRootFileProvider;
        this.cache = cache;
        this.logger = logger;

        Releases = Load(files);

        if (env.IsDevelopment())
        {
            watcher = ChangeToken.OnChange(() => files.Watch($"{DirectoryPath}/*.md"), Reload);
        }
    }

    private void Reload()
    {
        try
        {
            Releases = Load(files);
        }
        catch (Exception ex)
        {
            // Editors can trigger this mid-save, the next save reloads again
            logger.LogError(ex, "Failed to reload music releases, keeping the previous ones");
            return;
        }

        logger.LogInformation("Reloaded {Count} music releases", Releases.Count);
        _ = cache.EvictByTagAsync(CacheTag, CancellationToken.None).AsTask();
    }

    internal static IReadOnlyList<MusicRelease> Load(IFileProvider files)
    {
        var directory = files.GetDirectoryContents(DirectoryPath);

        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"Music directory '{DirectoryPath}' not found in the content root.");
        }

        return directory
            .Where(x => !x.IsDirectory && x.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && !x.Name.StartsWith('_'))
            .Select(x =>
            {
                using var reader = new StreamReader(x.CreateReadStream());
                return Parse(x.Name, reader.ReadToEnd());
            })
            .OrderByDescending(x => x.Released)
            .ThenBy(x => x.Title)
            .ToList();
    }

    internal static MusicRelease Parse(string fileName, string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        var frontMatterBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault()
            ?? throw new InvalidDataException($"{fileName}: missing front matter, the file has to start with a --- block.");

        FrontMatter frontMatter;

        try
        {
            frontMatter = Yaml.Deserialize<FrontMatter?>(frontMatterBlock.Lines.ToString()) ?? new();
        }
        catch (YamlException ex)
        {
            throw new InvalidDataException($"{fileName}: invalid front matter. {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(frontMatter.Title))
        {
            throw new InvalidDataException($"{fileName}: 'title' is required.");
        }

        if (!DateOnly.TryParseExact(frontMatter.Released, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var released))
        {
            throw new InvalidDataException($"{fileName}: 'released' is required in the yyyy-MM-dd format.");
        }

        var type = frontMatter.Type switch
        {
            null or "original" => MusicReleaseType.Original,
            "remix" => MusicReleaseType.Remix,
            _ => throw new InvalidDataException($"{fileName}: 'type' has to be 'original' or 'remix'.")
        };

        return new MusicRelease(frontMatter.Title, released, type, ParseEmbed(fileName, frontMatter), document.ToHtml(Pipeline));
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

    public void Dispose()
    {
        watcher?.Dispose();
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
