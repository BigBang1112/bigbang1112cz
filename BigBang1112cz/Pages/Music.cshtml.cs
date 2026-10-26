using BigBang1112cz.Extensions;
using BigBang1112cz.Models.Music;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages;

[OutputCache(Duration = 3600, Tags = [MusicCatalog.CacheTag])]
public class MusicModel : PageModel
{
    private readonly MusicCatalog catalog;
    private readonly TimeProvider timeProvider;

    public IReadOnlyList<MusicRelease> Releases { get; private set; } = [];
    public DateOnly Today { get; private set; }

    public MusicModel(MusicCatalog catalog, TimeProvider timeProvider)
    {
        this.catalog = catalog;
        this.timeProvider = timeProvider;
    }

    public void OnGet()
    {
        Releases = catalog.Releases;
        Today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        Response.ClientCache();
    }
}
