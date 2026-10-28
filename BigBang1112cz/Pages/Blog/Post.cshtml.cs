using BigBang1112cz.Extensions;
using BigBang1112cz.Models.Blog;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages.Blog;

[OutputCache(Duration = 3600, Tags = [BlogCatalog.CacheTag])]
public class BlogPostModel : PageModel
{
    private readonly BlogCatalog catalog;
    private readonly TimeProvider timeProvider;
    private readonly IWebHostEnvironment env;

    public BlogPost Post { get; private set; } = null!;
    public bool IsScheduled { get; private set; }

    public BlogPostModel(BlogCatalog catalog, TimeProvider timeProvider, IWebHostEnvironment env)
    {
        this.catalog = catalog;
        this.timeProvider = timeProvider;
        this.env = env;
    }

    public IActionResult OnGet(string slug)
    {
        var now = timeProvider.GetUtcNow();

        if (catalog.Find(slug, now, includeScheduled: env.IsDevelopment()) is not { } post)
        {
            return NotFound();
        }

        Post = post;
        IsScheduled = post.Published > now;
        Response.ClientCache();

        return Page();
    }
}
