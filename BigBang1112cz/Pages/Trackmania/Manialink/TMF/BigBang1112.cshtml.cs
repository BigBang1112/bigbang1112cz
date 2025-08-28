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

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF;

[OutputCache(Duration = 3600, VaryByQueryKeys = ["P", "LocatorHost"], Tags = ["comments", "downloads"])]
public class BigBang1112Model : XmlPageModel
{
    private readonly AppDbContext db;
    private readonly IOptions<TrackmaniaOptions> options;
    public const int ResultsPerPage = 10;

    [FromQuery(Name = "P")]
    [Range(1, 20)] // hardcoded 20 page limit for now to prevent abuse
    public int PageNum { get; set; } = 1;

    [FromQuery]
    public HostType LocatorHost { get; set; }

    public int MaxPageNum { get; set; }
    public int NextPageNum { get; set; }
    public int PrevPageNum { get; set; }
    public List<HornModel> Horns { get; set; } = [];

    public int DownloadTotalCount { get; set; }
    public int CommentTotalCount { get; set; }

    public BigBang1112Model(AppDbContext db, IOptions<TrackmaniaOptions> options, IWebHostEnvironment env) : base(env)
    {
        this.db = db;
        this.options = options;
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

        var hornCount = await db.Horns.CountAsync(cancellationToken);

        MaxPageNum = (int)Math.Ceiling((double)hornCount / ResultsPerPage);
        PageNum = Math.Clamp(PageNum, 1, MaxPageNum);

        var horns = await db.Horns
            .OrderBy(x => x.FileName)
            .Where(x => !x.IsDeleted)
            .Skip((PageNum - 1) * ResultsPerPage)
            .Take(ResultsPerPage)
            .Select(x => new
            {
                x.FileName,
                x.Duration,
                x.LastModifiedAt,
                CommentCount = x.Comments.Count
            })
            .ToListAsync(cancellationToken);

        NextPageNum = PageNum < MaxPageNum ? PageNum + 1 : 1;
        PrevPageNum = PageNum > 1 ? PageNum - 1 : MaxPageNum;

        Horns = horns.Select(x => new HornModel
        {
            Name = x.FileName,
            Duration = x.Duration,
            IsNew = false,// x.LastModifiedAt > DateTime.UtcNow.AddDays(-30), doesnt work now
            CommentCount = x.CommentCount,
        }).ToList();

        DownloadTotalCount = await db.HornDownloads.CountAsync(cancellationToken);
        CommentTotalCount = await db.HornComments.CountAsync(cancellationToken);

        Response.ClientCache();

        return Page();
    }
}
