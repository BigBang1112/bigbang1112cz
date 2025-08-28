using BigBang1112cz.Extensions;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages.Trackmania.Manialink.MP.Minigames;

[OutputCache(Duration = 3600)]
public class BreakoutModel : XmlPageModel
{
    public BreakoutModel(IHostEnvironment env) : base(env)
    {
    }

    public void OnGet()
    {
        Response.ClientCache();
    }
}
