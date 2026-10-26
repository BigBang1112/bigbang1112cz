using BigBang1112cz.Data;
using BigBang1112cz.Models.Db;
using BigBang1112cz.Options;
using BigBang1112cz.Pages.Shared;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF.BigBang1112;

public class BulkModel : TmfPlayerPageModel
{
    private readonly HornUserService userService;
    private readonly AppDbContext db;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<BulkModel> logger;

    public List<HornDbModel> Horns { get; set; } = [];

    public BulkModel(
        HornUserService userService,
        AppDbContext db,
        IOptions<TrackmaniaOptions> options,
        IOutputCacheStore cache,
        IWebHostEnvironment env,
        ILogger<BulkModel> logger) : base(options, env, logger)
    {
        this.userService = userService;
        this.db = db;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        Horns = await db.Horns
            .Where(x => !x.IsDeleted)
            .OrderBy(x => EF.Functions.Random())
            .Take(10)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Bulk download request by {Nickname} (login: {Login}) is valid", DeformattedNickname, Login);

        foreach (var horn in Horns)
        {
            logger.LogInformation("Picked {Horn} for bulk download by {Nickname} (login: {Login})", horn.FileName, DeformattedNickname, Login);
        }

        var user = await userService.GetOrUpdateUserAsync(Login, Nickname, Zone, cancellationToken);

        await db.HornDownloads.AddRangeAsync(Horns.Select(x => new HornDownloadDbModel
        {
            User = user,
            Horn = x,
            DownloadedAt = DateTime.UtcNow,
        }), cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await cache.EvictByTagAsync("downloads", CancellationToken.None);

        return Page();
    }
}
