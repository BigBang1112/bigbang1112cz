using BigBang1112cz.Extensions;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages.Trackmania.Manialink.MP;

[OutputCache(Duration = 3600)]
public class BigBang1112Model : XmlPageModel
{
    public BigBang1112Model(IHostEnvironment env) : base(env)
    {
    }

    public void OnGet()
    {
        Response.ClientCache();
    }
}
