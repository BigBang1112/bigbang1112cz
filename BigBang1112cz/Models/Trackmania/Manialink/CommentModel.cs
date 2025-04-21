namespace BigBang1112cz.Models.Trackmania.Manialink;

public sealed class CommentModel
{
    public required string Content { get; set; }
    public required string Nickname { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
