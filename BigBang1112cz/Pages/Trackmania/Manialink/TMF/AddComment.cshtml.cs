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

public class AddCommentModel : XmlPageModel
{
    private readonly HornUserService userService;
    private readonly AppDbContext db;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<AddCommentModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    [FromQuery(Name = "FromP")]
    public int FromPageNum { get; set; }

    [FromQuery]
    [StringLength(255)]
    public required string Nickname { get; set; }

    [FromQuery(Name = "playerlogin")]
    [StringLength(255, MinimumLength = 2)]
    public required string Login { get; set; }

    [FromQuery(Name = "path")]
    [StringLength(255)]
    public required string Zone { get; set; }

    [FromQuery]
    [StringLength(96)]
    public required string Comment { get; set; }

    [FromQuery]
    public HostType LocatorHost { get; set; }

    public string? Message { get; set; }

    public required string Link { get; set; }

    public AddCommentModel(
        HornUserService userService, 
        AppDbContext db, 
        IOutputCacheStore cache,
        IWebHostEnvironment env, 
        ILogger<AddCommentModel> logger) : base(env)
    {
        this.userService = userService;
        this.db = db;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var deformattedNickname = TextFormatter.Deformat(Nickname, maxReplacementCount: 1000);

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

        Link = ModelState.IsValid
            ? ManialinkUrl("bigbang1112:confirmcomment") + Request.QueryString
            : ManialinkUrl("bigbang1112:comments") + $"?horn={Horn}&fromp={FromPageNum}&commentp=1&locatorhost={LocatorHost}";

        if (!ModelState.IsValid)
        {
            var commentState = ModelState["Comment"];

            if (commentState is not null && commentState.Errors.Count > 0)
            {
                Message = commentState.Errors[0].ErrorMessage;
            }
            else
            {
                Message = "Comment is not valid.";
            }

            return Page();
        }

        return Page();
    }
}
