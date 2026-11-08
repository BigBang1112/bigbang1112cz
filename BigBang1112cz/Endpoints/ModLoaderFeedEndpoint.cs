using BigBang1112cz.Services;
using BigBang1112cz.Models.ModLoader;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;

namespace BigBang1112cz.Endpoints;

public static class ModLoaderFeedEndpoint
{
    public const string Path = "/trackmania/tmf/modloader/index.json";

    public static void MapModLoaderFeed(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, async Task<Results<Ok<ModLoaderFeed>, ProblemHttpResult>> (
            IModLoaderFeedService service, ILogger<ModLoaderFeedService> logger, CancellationToken cancellationToken) =>
        {
            try
            {
                return TypedResults.Ok(await service.GetAsync(cancellationToken));
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException ||
                ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Could not generate the ModLoader feed from GitHub releases");
                return TypedResults.Problem(statusCode: StatusCodes.Status502BadGateway,
                    title: "The ModLoader feed is temporarily unavailable.");
            }
        })
        .WithName("ModLoaderFeed")
        .WithSummary("Latest stable Always Official release for TMForever ModLoader")
        .CacheOutput(policy => policy.Expire(TimeSpan.FromHours(1)));
    }
}
