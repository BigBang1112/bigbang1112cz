using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF;

[OutputCache(Duration = 3600, VaryByQueryKeys = ["P"], Tags = ["comments", "downloads"])]
public class IndexModel : XmlPageModel
{
    private readonly IWebHostEnvironment env;

    public const int ResultsPerPage = 10;

    [FromQuery(Name = "P")]
    public int PageNum { get; set; } = 1;

    public int MaxPageNum { get; set; }
    public int NextPageNum { get; set; }
    public int PrevPageNum { get; set; }
    public List<HornModel> Horns { get; set; } = [];

    public int DownloadTotalCount { get; set; }
    public int CommentTotalCount { get; set; }

    public IndexModel(IWebHostEnvironment env) : base(env)
    {
        this.env = env;
    }

    public IActionResult OnGet()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var horns = env.WebRootFileProvider.GetDirectoryContents("horns");

        MaxPageNum = (int)Math.Ceiling((double)horns.Count() / ResultsPerPage);

        PageNum = Math.Clamp(PageNum, 1, MaxPageNum);
        NextPageNum = PageNum < MaxPageNum ? PageNum + 1 : 1;
        PrevPageNum = PageNum > 1 ? PageNum - 1 : MaxPageNum;

        foreach (var hornFile in horns.OrderBy(x => x.Name).Skip((PageNum - 1) * ResultsPerPage).Take(ResultsPerPage))
        {
            var oggFile = TagLib.File.Create(hornFile.PhysicalPath);
            var duration = oggFile.Properties.Duration;

            Horns.Add(new HornModel
            {
                Name = hornFile.Name,
                Duration = duration,
                IsNew = hornFile.LastModified > DateTime.UtcNow.AddDays(-30),
                CommentCount = 0
            });
        }

        return Page();
    }
}
