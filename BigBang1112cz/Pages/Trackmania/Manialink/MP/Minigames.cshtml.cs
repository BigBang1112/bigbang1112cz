using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BigBang1112cz.Pages.Trackmania.Manialink.MP;

public class MinigamesModel : XmlPageModel
{
    public MinigamesModel(IHostEnvironment env) : base(env)
    {
    }

    public IActionResult OnGet()
    {
        var query = Request.QueryString.Value?.TrimStart('?');

        if (string.IsNullOrEmpty(query))
        {
            return Page();
        }

        if (Enum.TryParse<Minigame>(query, ignoreCase: true, out var minigame))
        {
            return RedirectToPagePermanent($"/Trackmania/Manialink/MP/Minigames/{minigame}");
        }

        return NotFound();
    }
}
