namespace BigBang1112cz.Models.Blog;

public sealed record BlogPost(
    string Slug,
    string Title,
    string Author,
    DateTimeOffset Published,
    string Description,
    int ReadingMinutes,
    string ContentHtml,
    string? OgImage = null,
    DateTimeOffset? Updated = null)
{
    /// <summary>
    /// The day of publishing in UTC, which is what the pages show.
    /// </summary>
    public DateOnly PublishedDate => DateOnly.FromDateTime(Published.UtcDateTime);

    /// <summary>
    /// The day of the last notable change in UTC, if the post was updated after publishing.
    /// </summary>
    public DateOnly? UpdatedDate => Updated.HasValue ? DateOnly.FromDateTime(Updated.Value.UtcDateTime) : null;

    /// <summary>
    /// When the post last changed, the update or else the publishing.
    /// </summary>
    public DateTimeOffset LastChanged => Updated ?? Published;
}
