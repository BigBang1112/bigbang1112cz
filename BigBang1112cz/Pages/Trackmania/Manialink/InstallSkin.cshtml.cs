using BigBang1112cz.Extensions;
using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Pages.Shared;
using BigBang1112cz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using TmEssentials;

namespace BigBang1112cz.Pages.Trackmania.Manialink;

[OutputCache(Duration = 60, VaryByQueryKeys = ["Type", "url"])]
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

    public InstallSkinModel(IHttpClientFactory httpClientFactory, IHostEnvironment env) : base(env)
    {
        http = httpClientFactory.CreateClient(ExternalHttpClient.Name);
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
        
        string? fileName;

        try
        {
            using var response = await http.HeadAsync(uri, cancellationToken);
            var contentDisposition = response.Content.Headers.ContentDisposition;
            fileName = SanitizeFileName(contentDisposition?.FileNameStar ?? contentDisposition?.FileName);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            ValidationProblemMessage = "URL could not be reached";
            return;
        }

        fileName ??= SanitizeFileName(uri.LocalPath);

        if (fileName is null)
        {
            ValidationProblemMessage = "File name could not be determined";
            return;
        }

        FileName = fileName;
        Uri = uri;

        Response.ClientCache();
    }

    private static string? SanitizeFileName(string? fileName)
    {
        // Strip quotes and any directory part so the remote server can't choose where the game writes the file
        fileName = fileName?.Trim().Trim('"');
        fileName = fileName?[(fileName.LastIndexOfAny(['/', '\\']) + 1)..];
        return string.IsNullOrWhiteSpace(fileName) || fileName is "." or ".." ? null : fileName;
    }
}
