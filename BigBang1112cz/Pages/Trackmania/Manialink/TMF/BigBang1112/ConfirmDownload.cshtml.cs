using BigBang1112cz.Data;
using BigBang1112cz.Models.Db;
using BigBang1112cz.Options;
using BigBang1112cz.Pages.Shared;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF.BigBang1112;

public class ConfirmDownloadModel : TmfPlayerPageModel
{
    private readonly HornUserService userService;
    private readonly AppDbContext db;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<ConfirmDownloadModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    public int DownloadCount { get; set; }

    public ConfirmDownloadModel(
        HornUserService userService,
        AppDbContext db,
        IOptions<TrackmaniaOptions> options,
        IOutputCacheStore cache,
        IWebHostEnvironment env,
        ILogger<ConfirmDownloadModel> logger) : base(options, env, logger)
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

        var horn = await db.Horns
            .FirstOrDefaultAsync(x => x.FileName.Equals(Horn, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (horn is null)
        {
            logger.LogWarning("Download request for {Horn} by {Nickname} (login: {Login}) failed: file not found", Horn, DeformattedNickname, Login);
            return NotFound();
        }

        Horn = horn.FileName;

        logger.LogInformation("Download request for {Horn} by {Nickname} (login: {Login}) is valid", Horn, DeformattedNickname, Login);

        DownloadCount = await db.HornDownloads
            .CountAsync(x => x.Horn.FileName == Horn, cancellationToken);

        var user = await userService.GetOrUpdateUserAsync(Login, Nickname, Zone, cancellationToken);

        await db.HornDownloads.AddAsync(new HornDownloadDbModel
        {
            User = user,
            Horn = horn,
            DownloadedAt = DateTime.UtcNow,
        }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await cache.EvictByTagAsync("downloads", CancellationToken.None);

        return Page();
    }
}
