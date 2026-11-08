using BigBang1112cz.Services;
using Microsoft.AspNetCore.ResponseCompression;

namespace BigBang1112cz.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services)
    {
        services.AddRazorPages();

        services.AddHttpClient();
        services.AddExternalHttpClient();
        services.AddHttpClient<IModLoaderFeedService, ModLoaderFeedService>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BigBang1112cz/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.text+json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddOutputCache();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
        });
    }
}
