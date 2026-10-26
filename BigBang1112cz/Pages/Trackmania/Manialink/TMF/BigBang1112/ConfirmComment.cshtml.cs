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

public class ConfirmCommentModel : TmfPlayerPageModel
{
    private readonly HornUserService userService;
    private readonly AppDbContext db;
    private readonly IOutputCacheStore cache;
    private readonly ILogger<ConfirmCommentModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    [FromQuery]
    [StringLength(96)]
    public required string Comment { get; set; }

    public string? Message { get; set; }

    public ConfirmCommentModel(
        HornUserService userService,
        AppDbContext db,
        IOptions<TrackmaniaOptions> options,
        IOutputCacheStore cache,
        IWebHostEnvironment env,
        ILogger<ConfirmCommentModel> logger) : base(options, env, logger)
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
            logger.LogWarning("Comment post {Horn} by {Nickname} (login: {Login}) failed: horn not found", Horn, DeformattedNickname, Login);
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
            logger.LogInformation("Comment posted for {Horn} by {Nickname} (login: {Login})", Horn, DeformattedNickname, Login);

            Message = "Comment posted successfully!";

            await cache.EvictByTagAsync("comments", CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save comment for {Horn} by {Nickname} (login: {Login})", Horn, DeformattedNickname, Login);
            Message = "Something went wrong posting the message. Owner of the server has been notified.";
        }

        return Page();
    }
}
