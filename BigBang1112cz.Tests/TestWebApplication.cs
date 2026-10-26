using BigBang1112cz.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Core.Interfaces;

namespace BigBang1112cz.Tests;

/// <summary>
/// The real app with MySQL swapped for an in-memory database. Each instance gets its own database and output cache.
/// Horns are seeded from wwwroot/horns by HornHostedService on startup, like in production.
/// </summary>
public class TestWebApplication : WebApplicationFactory<Program>, IAsyncInitializer
{
    private readonly string databaseName = Guid.NewGuid().ToString();

    public Task InitializeAsync()
    {
        // Starts the host so HornHostedService seeds the horns before any test runs
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));

            ConfigureTestServices(services);
        });
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    public HttpClient CreateGameBoxClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GameBox");
        return client;
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task WithDbAsync(Func<AppDbContext, Task> action) => WithDbAsync(async db =>
    {
        await action(db);
        return true;
    });

    public async Task EvictCacheAsync(params string[] tags)
    {
        var cache = Services.GetRequiredService<IOutputCacheStore>();

        foreach (var tag in tags)
        {
            await cache.EvictByTagAsync(tag, CancellationToken.None);
        }
    }
}
