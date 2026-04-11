using BigBang1112cz.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages;

[OutputCache(Duration = 3600)]
public class ContactModel : PageModel
{
    public void OnGet()
    {
        Response.ClientCache();
    }
}
