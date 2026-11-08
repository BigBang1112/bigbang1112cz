using BigBang1112cz.Models.ModLoader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigBang1112cz.Services;

public interface IModLoaderFeedService
{
    Task<ModLoaderFeed> GetAsync(CancellationToken cancellationToken);
}

public sealed class ModLoaderFeedService(HttpClient http) : IModLoaderFeedService
{
    internal static readonly JsonSerializerOptions GitHubJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<ModLoaderFeed> GetAsync(CancellationToken cancellationToken)
    {
        var releases = new List<GitHubRelease>();

        // GitHub returns at most 100 releases per page, including prereleases.
        for (var page = 1; ; page++)
        {
            var items = await http.GetFromJsonAsync<GitHubRelease[]>(
                $"repos/BigBang1112/always-official/releases?per_page=100&page={page}",
                GitHubJsonOptions, cancellationToken)
                ?? throw new JsonException("GitHub returned no release list.");
            releases.AddRange(items);
            if (items.Length < 100)
            {
                break;
            }
        }

        return Build(releases);
    }

    internal static ModLoaderFeed Build(IEnumerable<GitHubRelease> releases)
    {
        var release = releases
            .Where(x => !x.Draft && !x.Prerelease && x.ModLoaderArchive is not null)
            .MaxBy(x => x.PublishedAt);

        if (release is null)
        {
            return new([]);
        }

        var archive = release.ModLoaderArchive!;
        var digest = archive.Digest;
        if (digest is null || !digest.StartsWith("sha256:", StringComparison.Ordinal) ||
            digest.Length != 71 || digest.AsSpan(7).ContainsAnyExcept("0123456789abcdefABCDEF"))
        {
            throw new JsonException("The ModLoader archive has no valid SHA-256 digest.");
        }

        var version = new ModLoaderVersion(
            "AlwaysOfficial.dll",
            [new("CoreMod", "^1.0.1")],
            archive.BrowserDownloadUrl,
            digest[7..],
            string.IsNullOrWhiteSpace(release.BodyText) ? null : release.BodyText);

        return new(new()
        {
            ["Always Official"] = new(
                "Always Official",
                "BigBang1112",
                "modification",
                "After entering official mode manually, restart subsequent solo runs in official mode without repeating the confirmation.",
                "https://github.com/BigBang1112/always-official",
                new() { [release.Version] = version })
        });
    }

    internal sealed record GitHubRelease(
        string TagName,
        DateTimeOffset? PublishedAt,
        bool Draft,
        bool Prerelease,
        string? BodyText,
        GitHubAsset[] Assets)
    {
        [JsonIgnore]
        public string Version => TagName.StartsWith('v') ? TagName[1..] : TagName;

        [JsonIgnore]
        public GitHubAsset? ModLoaderArchive =>
            Assets.FirstOrDefault(x => x.Name == $"AlwaysOfficial-modloader.v{Version}.zip") ??
            Assets.FirstOrDefault(x => x.Name == "AlwaysOfficial-modloader.zip");
    }

    internal sealed record GitHubAsset(string Name, string? Digest, string BrowserDownloadUrl);
}
