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

    public IReadOnlyList<MusicRelease> Releases { get; private set; } = [];

    public MusicModel(MusicCatalog catalog)
    {
        this.catalog = catalog;
    }

    public void OnGet()
    {
        Releases = catalog.Releases;
        Response.ClientCache();
    }
}
