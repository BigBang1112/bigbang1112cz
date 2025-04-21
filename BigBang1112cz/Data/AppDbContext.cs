using BigBang1112cz.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112cz.Data;

public class AppDbContext : DbContext
{
    public DbSet<HornDbModel> Horns { get; set; }
    public DbSet<HornCommentDbModel> HornComments { get; set; }
    public DbSet<HornUserDbModel> HornUsers { get; set; }
    public DbSet<HornDownloadDbModel> HornDownloads { get; set; }
    public DbSet<HornUserHistoryDbModel> HornUserHistory { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}