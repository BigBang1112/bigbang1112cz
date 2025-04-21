using BigBang1112cz.Services;

namespace BigBang1112cz.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<HornUserService>();
        services.AddHostedService<HornHostedService>();
    }
}
