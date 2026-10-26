using BigBang1112cz.Data;
using BigBang1112cz.Options;
using BigBang1112cz.Pages.Shared;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF.BigBang1112;

public class DownloadModel : TmfPlayerPageModel
{
    private readonly AppDbContext db;
    private readonly HttpClient http;
    private readonly ILogger<DownloadModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    public string? Message { get; set; }

    public required string Link { get; set; }

    public DownloadModel(
        AppDbContext db,
        HttpClient http,
        IOptions<TrackmaniaOptions> options,
        IWebHostEnvironment env,
        ILogger<DownloadModel> logger) : base(options, env, logger)
    {
        this.db = db;
        this.http = http;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var horn = await db.Horns
            .FirstOrDefaultAsync(x => x.FileName.Equals(Horn, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (horn is null)
        {
            logger.LogWarning("Download request for {Horn} by {Nickname} (login: {Login}) failed: horn not found", Horn, DeformattedNickname, Login);
            return NotFound();
        }

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
