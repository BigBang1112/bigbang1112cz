using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112cz.Pages.Trackmania.Manialink.TMF;

public class CommentsModel : XmlPageModel
{
    private readonly IWebHostEnvironment env;
    private readonly ILogger<CommentsModel> logger;

    [FromQuery]
    [StringLength(100, MinimumLength = 5)]
    public required string Horn { get; set; }

    [FromQuery(Name = "fromp")]
    public int FromPageNum { get; set; }

    public CommentsModel(IWebHostEnvironment env, ILogger<CommentsModel> logger) : base(env)
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

        var hornFile = env.WebRootFileProvider
            .GetDirectoryContents("horns")
            .FirstOrDefault(x => x.Name.Equals(Horn, StringComparison.OrdinalIgnoreCase));

        if (hornFile is null)
        {
            logger.LogWarning("Horn {Horn} file not found", Horn);
            return NotFound();
        }

        return Page();
    }
}
