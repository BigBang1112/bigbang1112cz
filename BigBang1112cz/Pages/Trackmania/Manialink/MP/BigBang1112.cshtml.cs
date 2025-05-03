using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages.Trackmania.Manialink.MP;

[OutputCache]
public class BigBang1112Model : XmlPageModel
{
    public BigBang1112Model(IHostEnvironment env) : base(env)
    {
    }

    public void OnGet()
    {
    }
}
