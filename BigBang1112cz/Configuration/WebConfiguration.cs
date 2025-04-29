using Microsoft.AspNetCore.ResponseCompression;

namespace BigBang1112cz.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services)
    {
        services.AddRazorPages();

        services.AddHttpClient();

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
