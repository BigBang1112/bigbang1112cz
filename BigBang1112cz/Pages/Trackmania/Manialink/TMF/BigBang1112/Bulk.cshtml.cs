using BigBang1112cz.Data;
using BigBang1112cz.Models.Db;
using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Options;
using BigBang1112cz.Pages.Shared;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF.BigBang1112;

public class BulkModel : XmlPageModel
{
    private readonly HornUserService userService;
    private readonly AppDbContext db;
    private readonly IOptions<TrackmaniaOptions> options;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<BulkModel> logger;

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

    public List<HornDbModel> Horns { get; set; } = [];

    public BulkModel(
        HornUserService userService, 
        AppDbContext db,
        IOptions<TrackmaniaOptions> options,
        IOutputCacheStore cache,
        IWebHostEnvironment env, 
        ILogger<BulkModel> logger) : base(env)
    {
        this.userService = userService;
        this.db = db;
        this.options = options;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (options.Value.ManialinkRegistrationMode)
        {
            return StatusCode(200);
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var deformattedNickname = Nickname is null ? null : TextFormatter.Deformat(Nickname, maxReplacementCount: 1000);

        if (!Request.GetTypedHeaders().Headers.UserAgent.Equals("GameBox"))
        {
            logger.LogWarning("Bulk download request by {Nickname} (login: {Login}) from non-GameBox client", deformattedNickname, Login);
            return BadRequest();
        }

        Horns = await db.Horns
            .OrderBy(x => EF.Functions.Random())
            .Take(10)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Bulk download request by {Nickname} (login: {Login}) is valid", deformattedNickname, Login);

        foreach (var horn in Horns)
        {
            logger.LogInformation("Picked {Horn} for bulk download by {Nickname} (login: {Login})", horn.FileName, deformattedNickname, Login);
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
