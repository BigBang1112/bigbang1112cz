using BigBang1112cz.Data;
using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Pages.Shared;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF;

public class DownloadModel : XmlPageModel
{
    private readonly HornUserService userService;
    private readonly AppDbContext db;
    private readonly HttpClient http;
    private readonly ILogger<DownloadModel> logger;

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

    public string? Message { get; set; }

    public required string Link { get; set; }

    public DownloadModel(
        HornUserService userService, 
        AppDbContext db,
        HttpClient http,
        IWebHostEnvironment env, 
        ILogger<DownloadModel> logger) : base(env)
    {
        this.userService = userService;
        this.db = db;
        this.http = http;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var deformattedNickname = Nickname is null ? null : TextFormatter.Deformat(Nickname, maxReplacementCount: 1000);

        if (!Request.GetTypedHeaders().Headers.UserAgent.Equals("GameBox"))
        {
            logger.LogWarning("Comment post on {Horn} by {Nickname} (login: {Login}) from non-GameBox client", Horn, deformattedNickname, Login);
            return BadRequest();
        }

        var horn = await db.Horns
            .FirstOrDefaultAsync(x => x.FileName.Equals(Horn, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (horn is null)
        {
            logger.LogWarning("Comment post {Horn} by {Nickname} (login: {Login}) failed: horn not found", Horn, deformattedNickname, Login);
            return NotFound();
        }

        var user = await userService.GetOrUpdateUserAsync(Login, Nickname, Zone, cancellationToken);

        using var hornResponse = await http.HeadAsync(HornLocatorHost.GetUrl(Request, LocatorHost, horn.FileName), cancellationToken);

        var isValid = ModelState.IsValid && hornResponse.IsSuccessStatusCode;
        if (isValid)
        {
            Link = ManialinkUrl("bigbang1112:confirmdownload") + Request.QueryString;
        }
        else
        {
            Message = "The horn is not available on this locator host.";
            Link = ManialinkUrl("bigbang1112") + $"?p={FromPageNum}&locatorhost={LocatorHost}";
        }

        return Page();
    }
}
