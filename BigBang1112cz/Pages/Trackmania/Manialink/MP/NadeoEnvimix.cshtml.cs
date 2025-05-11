using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BigBang1112cz.Pages.Trackmania.Manialink.MP;

public class NadeoEnvimixModel : XmlPageModel
{
    public NadeoEnvimixType Type { get; set; }

    public NadeoEnvimixModel(IHostEnvironment env) : base(env)
    {
    }

    public IActionResult OnGet()
    {
        var query = Request.QueryString.Value?.TrimStart('?');

        if (string.IsNullOrEmpty(query))
        {
            return Page();
        }

        if (!Enum.TryParse<NadeoEnvimixType>(query, ignoreCase: true, out var type))
        {
            return NotFound();
        }

        Type = type;

        return Page();
    }
}
