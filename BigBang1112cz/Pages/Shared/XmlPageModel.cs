using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

namespace BigBang1112cz.Pages.Shared;

public abstract class XmlPageModel : PageModel
{
    private readonly IHostEnvironment env;

    protected XmlPageModel(IHostEnvironment env)
    {
        this.env = env;
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        Response.ContentType = "application/xml";
        await next();
    }

    public string ManialinkUrl(string id, bool isMP = false)
    {
        if (env.IsProduction())
        {
            return id;
        }

        if (!id.Contains(':'))
        {
            return $"{Request.Scheme}://{Request.Host}/trackmania/manialink/{(isMP ? "mp" : "tmf")}/{id}";
        }

        return $"{Request.Scheme}://{Request.Host}/trackmania/manialink/{(isMP ? "mp" : "tmf")}/{string.Join('/', id.Split(':'))}";
    }
}