namespace BigBang1112cz.Models.Db;

public sealed class HornDownloadDbModel
{
    public int Id { get; set; }
    public required HornDbModel Horn { get; set; }
    public required HornUserDbModel User { get; set; }
    public required DateTimeOffset DownloadedAt { get; set; }
}
