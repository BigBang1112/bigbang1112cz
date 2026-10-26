using BigBang1112cz.Data;
using BigBang1112cz.Options;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF.BigBang1112;

public class AddCommentModel : TmfPlayerPageModel
{
    private readonly AppDbContext db;
    private readonly ILogger<AddCommentModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    [FromQuery]
    [StringLength(96)]
    public required string Comment { get; set; }

    public string? Message { get; set; }

    public required string Link { get; set; }

    public AddCommentModel(
        AppDbContext db,
        IOptions<TrackmaniaOptions> options,
        IWebHostEnvironment env,
        ILogger<AddCommentModel> logger) : base(options, env, logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var horn = await db.Horns
            .FirstOrDefaultAsync(x => x.FileName == Horn, cancellationToken);

        if (horn is null)
        {
            logger.LogWarning("Comment post {Horn} by {Nickname} (login: {Login}) failed: horn not found", Horn, DeformattedNickname, Login);
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            Link = ManialinkUrl("bigbang1112:confirmcomment") + Request.QueryString;
            return Page();
        }

        Link = ManialinkUrl("bigbang1112:comments") + $"?horn={Horn}&fromp={FromPageNum}&commentp=1&locatorhost={LocatorHost}";

        var commentState = ModelState["Comment"];

        Message = commentState is not null && commentState.Errors.Count > 0
            ? commentState.Errors[0].ErrorMessage
            : "Comment is not valid.";

        return Page();
    }
}
