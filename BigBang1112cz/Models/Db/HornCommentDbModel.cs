using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Models.Db;

public sealed class HornCommentDbModel
{
    public int Id { get; set; }

    public required HornDbModel Horn { get; set; }
    public required HornUserDbModel User { get; set; }

    [StringLength(96, MinimumLength = 1)]
    public required string Content { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
}
