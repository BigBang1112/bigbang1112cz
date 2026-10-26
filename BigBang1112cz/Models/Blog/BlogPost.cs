namespace BigBang1112cz.Models.Blog;

public sealed record BlogPost(
    string Slug,
    string Title,
    string Author,
    DateTimeOffset Published,
    string Description,
    int ReadingMinutes,
    string ContentHtml)
{
    /// <summary>
    /// The day of publishing in UTC, which is what the pages show.
    /// </summary>
    public DateOnly PublishedDate => DateOnly.FromDateTime(Published.UtcDateTime);
}
