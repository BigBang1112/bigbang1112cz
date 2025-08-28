using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Models.Db;

public sealed class HornUserHistoryDbModel
{
    public int Id { get; set; }

    public required HornUserDbModel User { get; set; }

    [StringLength(75)]
    public string? Nickname { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Zone { get; set; }

    public required DateTimeOffset LastSeenAt { get; set; }
}
