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

public class ConfirmCommentModel : XmlPageModel
{
    private readonly HornUserService userService;
    private readonly AppDbContext db;
    private readonly IOptions<TrackmaniaOptions> options;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<ConfirmCommentModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    [FromQuery(Name = "FromP")]
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
    [StringLength(96)]
    public required string Comment { get; set; }

    [FromQuery]
    public HostType LocatorHost { get; set; }

    public string? Message { get; set; }

    public ConfirmCommentModel(
        HornUserService userService, 
        AppDbContext db, 
        IOptions<TrackmaniaOptions> options,
        IOutputCacheStore cache,
        IWebHostEnvironment env, 
        ILogger<ConfirmCommentModel> logger) : base(env)
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

        await db.HornComments.AddAsync(new HornCommentDbModel
        {
            Content = Comment,
            CreatedAt = DateTime.UtcNow,
            User = user,
            Horn = horn
        }, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Comment posted for {Horn} by {Nickname} (login: {Login})", Horn, deformattedNickname, Login);

            Message = "Comment posted successfully!";

            await cache.EvictByTagAsync("comments", CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save comment for {Horn} by {Nickname} (login: {Login})", Horn, deformattedNickname, Login);
            Message = "Something went wrong posting the message. Owner of the server has been notified.";
        }

        return Page();
    }
}
