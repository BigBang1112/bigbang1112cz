using BigBang1112cz.Data;
using BigBang1112cz.Models.Db;
using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Pages.Shared;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF;

public class ConfirmDownloadModel : XmlPageModel
{
    private readonly HornUserService userService;
    private readonly AppDbContext db;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<ConfirmDownloadModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    [FromQuery(Name = "fromp")]
    public int FromPageNum { get; set; }

    [FromQuery]
    [StringLength(255)]
    public required string? Nickname { get; set; }

    [FromQuery(Name = "playerlogin")]
    [StringLength(255, MinimumLength = 2)]
    public required string Login { get; set; }

    [FromQuery(Name = "path")]
    [StringLength(255)]
    public required string? Zone { get; set; }

    [FromQuery]
    public HostType LocatorHost { get; set; }

    public int DownloadCount { get; set; }

    public ConfirmDownloadModel(
        HornUserService userService, 
        AppDbContext db,
        IOutputCacheStore cache,
        IWebHostEnvironment env, 
        ILogger<ConfirmDownloadModel> logger) : base(env)
    {
        this.userService = userService;
        this.db = db;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        return Page();
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var deformattedNickname = Nickname is null ? null : TextFormatter.Deformat(Nickname, maxReplacementCount: 1000);

        if (!Request.GetTypedHeaders().Headers.UserAgent.Equals("GameBox"))
        {
            logger.LogWarning("Download request for {Horn} by {Nickname} (login: {Login}) from non-GameBox client", Horn, deformattedNickname, Login);
            return BadRequest();
        }

        var horn = await db.Horns
            .FirstOrDefaultAsync(x => x.FileName.Equals(Horn, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (horn is null)
        {
            logger.LogWarning("Download request for {Horn} by {Nickname} (login: {Login}) failed: file not found", Horn, deformattedNickname, Login);
            return NotFound();
        }

        Horn = horn.FileName;

        logger.LogInformation("Download request for {Horn} by {Nickname} (login: {Login}) is valid", Horn, deformattedNickname, Login);

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
