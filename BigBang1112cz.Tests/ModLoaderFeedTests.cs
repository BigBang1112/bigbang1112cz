using BigBang1112cz.Endpoints;
using BigBang1112cz.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BigBang1112cz.Tests;

public class ModLoaderFeedTests
{
    private static JsonObject Release(string tag, string published, string? digest = null) => new()
    {
        ["tag_name"] = tag,
        ["published_at"] = published,
        ["draft"] = false,
        ["prerelease"] = false,
        ["body_text"] = "Release notes",
        ["assets"] = new JsonArray(new JsonObject
        {
            ["name"] = "AlwaysOfficial-modloader.zip",
            ["digest"] = digest ?? "sha256:" + new string('a', 64),
            ["browser_download_url"] = "https://github.com/BigBang1112/always-official/releases/download/example/mod.zip"
        })
    };

    [Test]
    public async Task Feed_SelectsLatestStableWithArchiveAndPrefersVersionedZip()
    {
        var stable = Release("v1.2.0", "2026-11-01T00:00:00Z");
        stable["assets"]!.AsArray().Add(new JsonObject
        {
            ["name"] = "AlwaysOfficial-modloader.v1.2.0.zip",
            ["digest"] = "sha256:" + new string('B', 64),
            ["browser_download_url"] = "https://example.com/versioned.zip"
        });
        var prerelease = Release("v2.0.0", "2026-11-02T00:00:00Z");
        prerelease["prerelease"] = true;
        var draft = Release("v3.0.0", "2026-11-03T00:00:00Z");
        draft["draft"] = true;
        var noArchive = Release("v4.0.0", "2026-11-04T00:00:00Z");
        noArchive["assets"] = new JsonArray();

        var feed = Build(Release("v1.0.0", "2026-10-01T00:00:00Z"), noArchive, stable, draft, prerelease);
        var versions = feed["products"]!["Always Official"]!["versions"]!.AsObject();
        await Assert.That(versions.Count).IsEqualTo(1);
        await Assert.That(versions["1.2.0"]!["download_link"]!.GetValue<string>()).IsEqualTo("https://example.com/versioned.zip");
        await Assert.That(versions["1.2.0"]!["hash_sha256"]!.GetValue<string>()).IsEqualTo(new string('B', 64));
        await Assert.That(versions["1.2.0"]!["changelog"]!.GetValue<string>()).IsEqualTo("Release notes");
    }

    [Test]
    public async Task Feed_SupportsLegacyArchiveAndOmitsBlankChangelog()
    {
        var release = Release("1.0.0", "2026-11-01T00:00:00Z");
        release["body_text"] = " \n";
        var version = Build(release)["products"]!["Always Official"]!["versions"]!["1.0.0"]!.AsObject();
        await Assert.That(version.ContainsKey("changelog")).IsFalse();
        await Assert.That(version["dependencies"]![0]!["version"]!.GetValue<string>()).IsEqualTo("^1.0.1");
    }

    [Test]
    public async Task Feed_WithoutReleasesHasEmptyProducts()
    {
        var feed = Build();
        await Assert.That(feed["api"]!.GetValue<int>()).IsEqualTo(1);
        await Assert.That(feed["game"]!.GetValue<string>()).IsEqualTo("TmForever");
        await Assert.That(feed["products"]!.AsObject().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Endpoint_ReturnsJsonAndCachesSuccessfulFeed()
    {
        using var app = new FeedWebApplication(new JsonArray(Release("v1.0.0", "2026-11-01T00:00:00Z")).ToJsonString());
        using var client = app.CreateClient();
        var response = await client.GetAsync(ModLoaderFeedEndpoint.Path);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo("application/json");
        var feed = await response.Content.ReadFromJsonAsync<JsonObject>();
        await Assert.That(feed!["products"]!.AsObject().ContainsKey("Always Official")).IsTrue();
        using var second = await client.GetAsync(ModLoaderFeedEndpoint.Path);
        await Assert.That(app.Handler.RequestCount).IsEqualTo(1);
    }

    [Test]
    public async Task Endpoint_InvalidDigestReturnsUncachedBadGateway()
    {
        using var app = new FeedWebApplication(new JsonArray(Release("v1.0.0", "2026-11-01T00:00:00Z", "sha256:invalid")).ToJsonString());
        using var client = app.CreateClient();
        using var response = await client.GetAsync(ModLoaderFeedEndpoint.Path);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadGateway);
        using var second = await client.GetAsync(ModLoaderFeedEndpoint.Path);
        await Assert.That(app.Handler.RequestCount).IsEqualTo(2);
    }

    private static JsonObject Build(params JsonObject[] releases) =>
        JsonSerializer.SerializeToNode(ModLoaderFeedService.Build(releases.Select(x =>
            x.Deserialize<ModLoaderFeedService.GitHubRelease>(ModLoaderFeedService.GitHubJsonOptions)!)),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();

    private sealed class FeedWebApplication(string json) : TestWebApplication
    {
        public FeedHandler Handler { get; } = new(json);

        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.AddHttpClient<IModLoaderFeedService, ModLoaderFeedService>()
                .ConfigurePrimaryHttpMessageHandler(() => Handler);
        }
    }

    private sealed class FeedHandler(string json) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
