using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages.Trackmania.Manialink.MP;

[OutputCache]
public class MapUidModel : XmlPageModel
{
    public MapUidModel(IHostEnvironment env) : base(env)
    {
    }

    public void OnGet()
    {
    }
}
