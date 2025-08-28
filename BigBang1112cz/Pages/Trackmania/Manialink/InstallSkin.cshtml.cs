using BigBang1112cz.Extensions;
using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using TmEssentials;

namespace BigBang1112cz.Pages.Trackmania.Manialink;

[OutputCache(Duration = 60, VaryByQueryKeys = ["Type", "uri"])]
public class InstallSkinModel : XmlPageModel
{
    private readonly HttpClient http;

    [FromQuery]
    public SkinType Type { get; set; }

    [FromQuery(Name = "url")]
    public required string UrlPath { get; set; }

    public string? ValidationProblemMessage { get; set; }

    public required string FileName { get; set; }
    public required Uri Uri { get; set; }

    public InstallSkinModel(HttpClient http, IHostEnvironment env) : base(env)
    {
        this.http = http;
    }

    public async Task OnGet(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UrlPath))
        {
            ValidationProblemMessage = "URL is required";
            return;
        }

        if (!Uri.TryCreate(UrlPath, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            ValidationProblemMessage = "URL is not valid";
            return;
        }
        
        using var response = await http.HeadAsync(uri, cancellationToken);

        FileName = response.Content.Headers.ContentDisposition?.FileName ?? Path.GetFileName(uri.LocalPath);
        Uri = uri;

        Response.ClientCache();
    }
}
