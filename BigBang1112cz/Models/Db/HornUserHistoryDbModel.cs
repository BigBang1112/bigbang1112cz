namespace BigBang1112cz.Models.Db;

public sealed class HornUserHistoryDbModel
{
    public int Id { get; set; }

    public required HornUserDbModel User { get; set; }
    public string? Nickname { get; set; }
    public string? Zone { get; set; }

    public required DateTimeOffset LastSeenAt { get; set; }
}
