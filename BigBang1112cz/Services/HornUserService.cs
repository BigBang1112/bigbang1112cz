using BigBang1112cz.Data;
using BigBang1112cz.Models.Db;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112cz.Services;

public sealed class HornUserService
{
    private readonly AppDbContext db;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<HornUserService> logger;

    public HornUserService(AppDbContext db, IOutputCacheStore cache, ILogger<HornUserService> logger)
    {
        this.db = db;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<HornUserDbModel> GetOrUpdateUserAsync(string login, string? nickname, string? zone, CancellationToken cancellationToken = default)
    {
        var user = await db.HornUsers
            .FirstOrDefaultAsync(x => x.Login.Equals(login, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (user is null)
        {
            user = new HornUserDbModel
            {
                Login = login,
                Nickname = nickname,
                Zone = zone,
                CreatedAt = DateTime.UtcNow,
            };
            await db.HornUsers.AddAsync(user, cancellationToken);
            await cache.EvictByTagAsync("users", CancellationToken.None);
        }
        else if (user.Nickname != nickname || user.Zone != zone)
        {
            logger.LogInformation("User {Login} nickname or zone changed from {OldNickname} ({OldZone}) to {NewNickname} ({NewZone})", login, user.Nickname, user.Zone, nickname, zone);

            await db.HornUserHistory.AddAsync(new HornUserHistoryDbModel
            {
                User = user,
                Nickname = user.Nickname,
                Zone = user.Zone,
                LastSeenAt = DateTime.UtcNow,
            }, cancellationToken);

            user.Nickname = nickname;
            user.Zone = zone;
            await cache.EvictByTagAsync("users", cancellationToken);
        }

        return user;
    }
}
