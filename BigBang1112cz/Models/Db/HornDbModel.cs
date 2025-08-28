using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Models.Db;

[Index(nameof(FileName), IsUnique = true)]
public sealed class HornDbModel
{
    public int Id { get; set; }

    [StringLength(100, MinimumLength = 5)]
    public required string FileName { get; set; }

    public required TimeSpan Duration { get; set; }
    public required DateTimeOffset LastModifiedAt { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<HornCommentDbModel> Comments { get; set; } = [];
}
