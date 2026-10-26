namespace BigBang1112cz.Models.Music;

public sealed record MusicRelease(
    string Title,
    DateOnly Released,
    MusicReleaseType Type,
    MusicEmbed? Embed,
    string ContentHtml);
