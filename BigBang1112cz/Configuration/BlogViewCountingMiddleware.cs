using BigBang1112cz.Services;
using System.Text.RegularExpressions;

namespace BigBang1112cz.Configuration;

/// <summary>
/// Counts blog post views. Post pages are output-cached, so this runs in front of the cache to see every request, also the ones the page itself never handles.
/// The site has no JavaScript of its own, so link previews, crawlers and other bots are filtered out by user agent.
/// </summary>
public static partial class BlogViewCountingMiddleware
{
    private const string PostPage = "/Blog/Post";

    public static void UseBlogViewCounting(this WebApplication app)
    {
        var counter = app.Services.GetRequiredService<BlogViewCounter>();

        app.Use(async (context, next) =>
        {
            // Routing has already picked the page, later middleware like the status code pages could change it
            var isPost = context.Request.RouteValues["page"] as string == PostPage;
            var slug = context.Request.RouteValues["slug"] as string;

            await next(context);

            if (isPost && slug is not null && IsReaderView(context))
            {
                counter.Count(slug.ToLowerInvariant(), Visitor(context));
            }
        });
    }

    internal static bool IsReaderView(HttpContext context)
    {
        var request = context.Request;

        // Revalidations of a page the browser already has come back as 304, they're still a visit
        if (!HttpMethods.IsGet(request.Method) || context.Response.StatusCode is not (StatusCodes.Status200OK or StatusCodes.Status304NotModified))
        {
            return false;
        }

        // Prefetched and prerendered pages may never be looked at
        if (request.Headers["Sec-Purpose"].ToString().Contains("prefetch") || request.Headers["Purpose"].ToString().Contains("prefetch"))
        {
            return false;
        }

        return IsBrowser(request.Headers.UserAgent.ToString());
    }

    internal static bool IsBrowser(string userAgent)
    {
        return userAgent.StartsWith("Mozilla/", StringComparison.Ordinal) && !NonBrowserRegex().IsMatch(userAgent);
    }

    private static string Visitor(HttpContext context)
    {
        // Behind a reverse proxy every request comes from the proxy, so the original address is preferred.
        // It can be faked, but that only lets someone count themselves more than once
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        var ip = forwardedFor.Length > 0
            ? forwardedFor.Split(',', 2)[0].Trim()
            : context.Connection.RemoteIpAddress?.ToString();

        return $"{ip}\n{context.Request.Headers.UserAgent}";
    }

    // Bots that pretend to be browsers usually still say what they are, often with a link to their info page
    [GeneratedRegex("bot|crawl|spider|slurp|preview|headless|lighthouse|monitor|scan|archive|feed|http|python|curl|wget", RegexOptions.IgnoreCase)]
    private static partial Regex NonBrowserRegex();
}
