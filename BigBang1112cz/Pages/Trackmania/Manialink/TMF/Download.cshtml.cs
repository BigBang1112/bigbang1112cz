using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF;

public class DownloadModel : XmlPageModel
{
    private readonly IWebHostEnvironment env;
    private readonly ILogger<DownloadModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    [FromQuery]
    [StringLength(255)]
    public required string Nickname { get; set; }

    [FromQuery(Name = "playerlogin")]
    [StringLength(255, MinimumLength = 2)]
    public required string Login { get; set; }

    [FromQuery(Name = "path")]
    [StringLength(255)]
    public required string Zone { get; set; }

    public int DownloadCount { get; set; }

    public DownloadModel(IWebHostEnvironment env, ILogger<DownloadModel> logger) : base(env)
    {
        this.env = env;
        this.logger = logger;
    }

    public IActionResult OnGet()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var deformattedNickname = TextFormatter.Deformat(Nickname, maxReplacementCount: 1000);

        if (!Request.GetTypedHeaders().Headers.UserAgent.Equals("GameBox"))
        {
            logger.LogWarning("Download request for {Horn} by {Nickname} (login: {Login}) from non-GameBox client", Horn, deformattedNickname, Login);
            return BadRequest();
        }

        var hornFile = env.WebRootFileProvider
            .GetDirectoryContents("horns")
            .FirstOrDefault(x => x.Name.Equals(Horn, StringComparison.OrdinalIgnoreCase));

        if (hornFile is null)
        {
            logger.LogWarning("Download request for {Horn} by {Nickname} (login: {Login}) failed: file not found", Horn, deformattedNickname, Login);
            return NotFound();
        }

        logger.LogInformation("Download request for {Horn} by {Nickname} (login: {Login}) is valid", Horn, deformattedNickname, Login);

        Horn = hornFile.Name;

        return Page();
    }
}
