using BigBang1112cz.Extensions;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages.Trackmania.Manialink.MP;

[OutputCache(Duration = 3600)]
public class MapUidModel : XmlPageModel
{
    public MapUidModel(IHostEnvironment env) : base(env)
    {
    }

    public void OnGet()
    {
        Response.ClientCache();
    }
}
