using BigBang1112cz.Data;
using BigBang1112cz.Extensions;
using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Options;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF.BigBang1112;

[OutputCache(Duration = 3600, VaryByQueryKeys = ["Horn", "CommentP"], Tags = ["comments", "users"])]
public class CommentsModel : XmlPageModel
{
    private readonly AppDbContext db;
    private readonly IOptions<TrackmaniaOptions> options;
    private readonly ILogger<CommentsModel> logger;

    public const int ResultsPerPage = 5;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    public string? Description { get; set; }

    [FromQuery(Name = "FromP")]
    [Range(1, 20)] // hardcoded 20 page limit for now to prevent abuse
    public int FromPageNum { get; set; } = 1;

    [FromQuery(Name = "CommentP")]
    public int CommentPageNum { get; set; } = 1;

    [FromQuery]
    public HostType LocatorHost { get; set; }

    public List<CommentModel> Comments { get; set; } = [];
    public int CommentCount { get; set; }

    public CommentsModel(AppDbContext db, IOptions<TrackmaniaOptions> options, IWebHostEnvironment env, ILogger<CommentsModel> logger) : base(env)
    {
        this.db = db;
        this.options = options;
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

        var horn = await db.Horns
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FileName.Equals(Horn, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (horn is null)
        {
            logger.LogWarning("Horn {Horn} file not found", Horn);
            return NotFound();
        }

        Comments = await db.HornComments
            .Where(x => x.Horn == horn)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((CommentPageNum - 1) * ResultsPerPage)
            .Take(ResultsPerPage)
            .Select(x => new CommentModel
            {
                Content = x.Content,
                Nickname = x.User.Nickname ?? x.User.Login,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        CommentCount = await db.HornComments.CountAsync(x => x.Horn == horn, cancellationToken);

        Horn = horn.FileName;
        Description = horn.Description;

        Response.ClientCache();

        return Page();
    }
}
