using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Models.Db;

/// <summary>
/// How many visitors saw a blog post on a day (UTC). Only the counts are stored, nothing about the visitors.
/// </summary>
[PrimaryKey(nameof(Slug), nameof(Date))]
public sealed class BlogPostViewDbModel
{
    public const int MaxSlugLength = 200;

    [StringLength(MaxSlugLength)]
    public required string Slug { get; set; }

    public required DateOnly Date { get; set; }

    public int Views { get; set; }
}
