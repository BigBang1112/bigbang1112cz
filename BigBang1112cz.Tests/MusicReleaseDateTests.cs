using BigBang1112cz.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.RegularExpressions;

namespace BigBang1112cz.Tests;

public sealed class FixedTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}

public class FixedTimeWebApplication : TestWebApplication
{
    public FixedTimeProvider Time { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(Time);
    }
}

// The tests move the shared clock, so they must not overlap
[ClassDataSource<FixedTimeWebApplication>(Shared = SharedType.PerClass)]
[NotInParallel]
public partial class MusicReleaseDateTests(FixedTimeWebApplication app)
{
    [Test]
    public async Task BeforeEveryRelease_AllSayReleases()
    {
        var labels = await GetLabelsAtAsync(new DateOnly(2000, 1, 1));

        await Assert.That(labels.Values).All().Satisfy(x => x.IsEqualTo("releases"));
    }

    [Test]
    public async Task AfterEveryRelease_AllSayReleased()
    {
        var labels = await GetLabelsAtAsync(new DateOnly(2100, 1, 1));

        await Assert.That(labels.Values).All().Satisfy(x => x.IsEqualTo("released"));
    }

    [Test]
    public async Task DayBeforeRelease_SaysReleases()
    {
        var newest = Newest();

        var labels = await GetLabelsAtAsync(newest.AddDays(-1));

        await Assert.That(labels[newest]).IsEqualTo("releases");
    }

    [Test]
    public async Task OnReleaseDay_SaysReleased()
    {
        var newest = Newest();

        var labels = await GetLabelsAtAsync(newest);

        await Assert.That(labels[newest]).IsEqualTo("released");
    }

    private DateOnly Newest() => app.Services.GetRequiredService<MusicCatalog>().Releases.Max(x => x.Released);

    /// <summary>
    /// Renders the music page as if it was the given day (UTC) and returns the label shown for each release date.
    /// </summary>
    private async Task<Dictionary<DateOnly, string>> GetLabelsAtAsync(DateOnly today)
    {
        app.Time.UtcNow = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await app.EvictCacheAsync(MusicCatalog.CacheTag);

        var html = await app.CreateClient().GetStringAsync("/music");

        return ReleaseLabel().Matches(html)
            .GroupBy(x => DateOnly.Parse(x.Groups["date"].Value))
            .ToDictionary(x => x.Key, x => x.First().Groups["label"].Value);
    }

    [GeneratedRegex("<time [^>]*datetime=\"(?<date>[0-9-]+)\"[^>]*>(?<label>\\w+) ")]
    private static partial Regex ReleaseLabel();
}
