using BigBang1112cz.Extensions;
using BigBang1112cz.Models.Blog;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages.Blog;

[OutputCache(Duration = 3600, Tags = [BlogCatalog.CacheTag])]
public class BlogIndexModel : PageModel
{
    private readonly BlogCatalog catalog;
    private readonly TimeProvider timeProvider;
    private readonly IWebHostEnvironment env;

    public IReadOnlyList<BlogPost> Posts { get; private set; } = [];
    public DateTimeOffset Now { get; private set; }

    public BlogIndexModel(BlogCatalog catalog, TimeProvider timeProvider, IWebHostEnvironment env)
    {
        this.catalog = catalog;
        this.timeProvider = timeProvider;
        this.env = env;
    }

    public void OnGet()
    {
        Now = timeProvider.GetUtcNow();
        Posts = catalog.GetVisible(Now, includeScheduled: env.IsDevelopment()).ToList();
        Response.ClientCache();
    }
}
