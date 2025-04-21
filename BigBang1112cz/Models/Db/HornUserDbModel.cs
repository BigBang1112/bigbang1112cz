namespace BigBang1112cz.Models.Db;

public sealed class HornUserDbModel
{
    public int Id { get; set; }

    public required string Login { get; set; }
    public string? Nickname { get; set; }
    public string? Zone { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public ICollection<HornCommentDbModel> Comments { get; set; } = [];
    public ICollection<HornUserHistoryDbModel> History { get; set; } = [];
}
