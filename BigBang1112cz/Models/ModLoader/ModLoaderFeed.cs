using System.Text.Json.Serialization;

namespace BigBang1112cz.Models.ModLoader;

/// <summary>A TM Forever ModLoader product feed.</summary>
public sealed record ModLoaderFeed(Dictionary<string, ModLoaderProduct> Products)
{
    [JsonPropertyOrder(-2)]
    public int Api => 1;

    [JsonPropertyOrder(-1)]
    public string Game => "TmForever";
}

/// <summary>A modification and its available versions.</summary>
public sealed record ModLoaderProduct(
    string Name,
    string Author,
    string Type,
    string Description,
    string Homepage,
    Dictionary<string, ModLoaderVersion> Versions);

/// <summary>A downloadable modification version.</summary>
public sealed record ModLoaderVersion(
    string Executable,
    ModLoaderDependency[] Dependencies,
    [property: JsonPropertyName("download_link")] string DownloadLink,
    [property: JsonPropertyName("hash_sha256")] string HashSha256,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Changelog);

/// <summary>A modification required by another modification.</summary>
public sealed record ModLoaderDependency(string Id, string Version);
