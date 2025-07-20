using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Models.Db;

[Index(nameof(Login), IsUnique = true)]
public sealed class HornUserDbModel
{
    public int Id { get; set; }

    [StringLength(75, MinimumLength = 1)]
    public required string Login { get; set; }

    [StringLength(75)]
    public string? Nickname { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Zone { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public ICollection<HornCommentDbModel> Comments { get; set; } = [];
    public ICollection<HornUserHistoryDbModel> History { get; set; } = [];
}
