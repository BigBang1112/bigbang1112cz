using BigBang1112cz.Configuration;
using BigBang1112cz.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace BigBang1112cz.Tests;

// Every test gets its own app, so views and remembered visitors don't carry over
[ClassDataSource<TestWebApplication>]
public class BlogViewTests(TestWebApplication app)
{
    private const string Firefox = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:144.0) Gecko/20100101 Firefox/144.0";
    private const string Chrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
    private const string Safari = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1";

    [Test]
    public async Task Post_CountsEachVisitorOncePerDay_AlsoFromCache()
    {
        var slug = Slug();

        await GetAsync($"/blog/{slug}", Firefox, "1.1.1.1");
        await GetAsync($"/blog/{slug}", Firefox, "1.1.1.1");
        await GetAsync($"/blog/{slug.ToUpperInvariant()}", Firefox, "1.1.1.1");

        // Served from the output cache
        await GetAsync($"/blog/{slug}", Firefox, "2.2.2.2");
        await GetAsync($"/blog/{slug}", Chrome, "2.2.2.2");

        await Assert.That(await ViewsAsync(slug)).IsEqualTo(3);
    }

    [Test]
    public async Task Post_AddsUpViewsAcrossSaves()
    {
        var slug = Slug();

        await GetAsync($"/blog/{slug}", Firefox, "1.1.1.1");
        await ViewsAsync(slug);
        await GetAsync($"/blog/{slug}", Firefox, "2.2.2.2");

        await Assert.That(await ViewsAsync(slug)).IsEqualTo(2);
    }

    [Test]
    public async Task Post_CountsVisitorsOfDifferentPostsSeparately()
    {
        var posts = app.Services.GetRequiredService<BlogCatalog>().Posts;

        await GetAsync($"/blog/{posts[0].Slug}", Firefox, "1.1.1.1");
        await GetAsync($"/blog/{posts[1].Slug}", Firefox, "1.1.1.1");

        await Assert.That(await ViewsAsync(posts[0].Slug)).IsEqualTo(1);
        await Assert.That(await ViewsAsync(posts[1].Slug)).IsEqualTo(1);
    }

    [Test]
    [Arguments("")]
    [Arguments("curl/8.9.1")]
    [Arguments("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [Arguments("Mozilla/5.0 (compatible; Discordbot/2.0; +https://discordapp.com)")]
    [Arguments("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)")]
    [Arguments("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/141.0.0.0 Safari/537.36")]
    public async Task Post_DoesNotCountBots(string userAgent)
    {
        var slug = Slug();

        await GetAsync($"/blog/{slug}", userAgent, "1.1.1.1");

        await Assert.That(await ViewsAsync(slug)).IsEqualTo(0);
    }

    [Test]
    public async Task Post_DoesNotCountPrefetches()
    {
        var slug = Slug();

        await GetAsync($"/blog/{slug}", Chrome, "1.1.1.1", ("Sec-Purpose", "prefetch;prerender"));

        await Assert.That(await ViewsAsync(slug)).IsEqualTo(0);
    }

    [Test]
    public async Task OtherBlogPages_AreNotCounted()
    {
        var response = await GetAsync("/blog/this-post-does-not-exist", Firefox, "1.1.1.1");
        await GetAsync("/blog", Firefox, "1.1.1.1");
        await GetAsync("/blog/feed.xml", Firefox, "1.1.1.1");

        await app.Services.GetRequiredService<BlogViewCounter>().FlushAsync();
        var rows = await app.WithDbAsync(db => db.BlogPostViews.CountAsync());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(rows).IsEqualTo(0);
    }

    [Test]
    [Arguments(Firefox)]
    [Arguments(Chrome)]
    [Arguments(Safari)]
    [Arguments("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0")]
    [Arguments("Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/28.0 Chrome/130.0.0.0 Mobile Safari/537.36")]
    public async Task Browsers_AreRecognized(string userAgent)
    {
        await Assert.That(BlogViewCountingMiddleware.IsBrowser(userAgent)).IsTrue();
    }

    private string Slug() => app.Services.GetRequiredService<BlogCatalog>().Posts[0].Slug;

    private async Task<HttpResponseMessage> GetAsync(string path, string userAgent, string ip, params (string Name, string Value)[] headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        request.Headers.Add("X-Forwarded-For", ip);

        foreach (var (name, value) in headers)
        {
            request.Headers.Add(name, value);
        }

        return await app.CreateClient().SendAsync(request);
    }

    private async Task<int> ViewsAsync(string slug)
    {
        await app.Services.GetRequiredService<BlogViewCounter>().FlushAsync();

        return await app.WithDbAsync(db => db.BlogPostViews.Where(x => x.Slug == slug).SumAsync(x => x.Views));
    }
}
