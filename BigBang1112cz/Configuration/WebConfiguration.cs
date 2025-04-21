using Microsoft.AspNetCore.ResponseCompression;

namespace BigBang1112cz.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services)
    {
        services.AddRazorPages();

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

    public static void UseSecurityMiddleware(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // app.UseHttpsRedirection(); needs to be avoided for TMF manialinks

        app.UseRouting();

        app.UseOutputCache();
        app.UseAuthorization();
        
        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();
    }
}
