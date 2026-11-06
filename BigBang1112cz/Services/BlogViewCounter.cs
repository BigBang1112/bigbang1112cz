using BigBang1112cz.Data;
using BigBang1112cz.Models.Db;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace BigBang1112cz.Services;

/// <summary>
/// Internal view counts of blog posts, one view per visitor per post per day (UTC).
/// Views are collected in memory and saved to the BlogPostViews table every minute and on shutdown, so requests never wait for the database.
/// Visitors are told apart by a hash of their IP address and user agent with a random key that is replaced every day,
/// kept only in memory, so nothing that identifies anyone is ever stored and yesterday's visitors can't be recognized.
/// </summary>
public sealed class BlogViewCounter : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(1);

    // About 3 MB of hashes. A flood beyond this is not real readers, so it stops being counted for the day
    internal const int MaxVisitorsPerDay = 100_000;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<BlogViewCounter> logger;

    private readonly Lock visitorLock = new();
    private readonly HashSet<Guid> visitors = [];
    private DateOnly visitorDay;
    private byte[] visitorKey = [];

    private readonly ConcurrentDictionary<(string Slug, DateOnly Date), int> pending = new();
    // Not disposed, as the host can dispose the service before the last save in StopAsync, and it never allocates anything to dispose
    private readonly SemaphoreSlim flushLock = new(1, 1);

    public BlogViewCounter(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<BlogViewCounter> logger)
    {
        this.scopeFactory = scopeFactory;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    /// <summary>
    /// Counts a view of the post unless this visitor already saw it today.
    /// </summary>
    /// <param name="visitor">Anything that tells visitors apart, like the IP address with the user agent. It is only hashed.</param>
    public void Count(string slug, string visitor)
    {
        if (slug.Length > BlogPostViewDbModel.MaxSlugLength)
        {
            return;
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        lock (visitorLock)
        {
            if (today != visitorDay)
            {
                visitors.Clear();
                visitorKey = RandomNumberGenerator.GetBytes(32);
                visitorDay = today;
            }

            if (visitors.Count >= MaxVisitorsPerDay)
            {
                return;
            }

            var hash = HMACSHA256.HashData(visitorKey, Encoding.UTF8.GetBytes($"{slug}\n{visitor}"));

            if (!visitors.Add(new Guid(hash.AsSpan(0, 16))))
            {
                return;
            }
        }

        pending.AddOrUpdate((slug, today), 1, (_, views) => views + 1);
    }

    /// <summary>
    /// Saves the views counted so far. If the database fails, they are kept for the next attempt.
    /// </summary>
    internal async Task FlushAsync()
    {
        await flushLock.WaitAsync();

        try
        {
            var batch = new List<((string Slug, DateOnly Date) Key, int Views)>();

            foreach (var key in pending.Keys)
            {
                if (pending.TryRemove(key, out var views))
                {
                    batch.Add((key, views));
                }
            }

            if (batch.Count == 0)
            {
                return;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                foreach (var ((slug, date), views) in batch)
                {
                    if (await db.BlogPostViews.FindAsync(slug, date) is { } row)
                    {
                        row.Views += views;
                    }
                    else
                    {
                        db.BlogPostViews.Add(new BlogPostViewDbModel { Slug = slug, Date = date, Views = views });
                    }
                }

                await db.SaveChangesAsync();

                logger.LogInformation("Saved {Views} blog post views", batch.Sum(x => x.Views));
            }
            catch (Exception ex)
            {
                foreach (var (key, views) in batch)
                {
                    pending.AddOrUpdate(key, views, (_, newer) => newer + views);
                }

                logger.LogError(ex, "Failed to save blog post views, trying again later");
            }
        }
        finally
        {
            flushLock.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync();
    }
}
