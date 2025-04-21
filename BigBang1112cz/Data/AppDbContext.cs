using BigBang1112cz.Models.Trackmania.Manialink;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112cz.Data;

public class AppDbContext : DbContext
{
    public DbSet<CommentModel> TrackmaniaManialinkTmfComments { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}