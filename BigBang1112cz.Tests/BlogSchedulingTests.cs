using BigBang1112cz.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace BigBang1112cz.Tests;

/// <summary>
/// The blog pages and the feed are cached for an hour, so the catalog has to clear the cache at the exact minute a scheduled post goes live.
/// </summary>
public class BlogSchedulingTests
{
    [Test]
    public async Task Cache_IsClearedExactlyWhenScheduledPostGoesLive()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 30, 18, 0, 0, TimeSpan.Zero));
        var cache = new RecordingCacheStore();
        using var content = new TempContent(("2026-10-30-post.md", "---\ntitle: Post\npublished: 2026-10-30 18:30\n---\n\nText."));
        using var catalog = new BlogCatalog(content.Environment, cache, time, NullLogger<BlogCatalog>.Instance);

        time.Advance(TimeSpan.FromMinutes(29));
        await Assert.That(cache.EvictedTags).IsEmpty();

        time.Advance(TimeSpan.FromMinutes(1));
        await Assert.That(cache.EvictedTags).IsEquivalentTo([BlogCatalog.CacheTag]);
    }

    [Test]
    public async Task Cache_IsClearedForPostsScheduledFarAhead()
    {
        // Longer than a single timer can wait (~49 days)
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 30, 18, 0, 0, TimeSpan.Zero));
        var cache = new RecordingCacheStore();
        using var content = new TempContent(("2027-01-30-post.md", "---\ntitle: Post\npublished: 2027-01-30 12:00\n---\n\nText."));
        using var catalog = new BlogCatalog(content.Environment, cache, time, NullLogger<BlogCatalog>.Instance);

        for (var now = time.GetUtcNow(); now < new DateTimeOffset(2027, 1, 30, 12, 0, 0, TimeSpan.Zero); now = time.GetUtcNow())
        {
            time.Advance(TimeSpan.FromHours(6));
        }

        var visible = catalog.GetVisible(time.GetUtcNow(), includeScheduled: false);

        await Assert.That(cache.EvictedTags).IsNotEmpty();
        await Assert.That(visible.Select(x => x.Slug)).IsEquivalentTo(["post"]);
    }

    [Test]
    public async Task Cache_IsNotClearedWhenNothingIsScheduled()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 30, 18, 0, 0, TimeSpan.Zero));
        var cache = new RecordingCacheStore();
        using var content = new TempContent(("2026-10-01-post.md", "---\ntitle: Post\npublished: 2026-10-01\n---\n\nText."));
        using var catalog = new BlogCatalog(content.Environment, cache, time, NullLogger<BlogCatalog>.Instance);

        time.Advance(TimeSpan.FromDays(10));

        await Assert.That(cache.EvictedTags).IsEmpty();
    }

    private sealed class RecordingCacheStore : IOutputCacheStore
    {
        public List<string> EvictedTags { get; } = [];

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            lock (EvictedTags)
            {
                EvictedTags.Add(tag);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken) => ValueTask.FromResult<byte[]?>(null);

        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    /// <summary>
    /// A content root with blog posts in a temporary directory, as a production environment so no files are watched.
    /// </summary>
    private sealed class TempContent : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "bigbang1112cz-tests-" + Guid.NewGuid().ToString("N"));
        private readonly PhysicalFileProvider files;

        public IWebHostEnvironment Environment { get; }

        public TempContent(params (string Name, string Markdown)[] posts)
        {
            var blog = Path.Combine(root, BlogCatalog.DirectoryPath);
            Directory.CreateDirectory(blog);

            foreach (var (name, markdown) in posts)
            {
                File.WriteAllText(Path.Combine(blog, name), markdown);
            }

            files = new PhysicalFileProvider(root);
            Environment = new TestEnvironment(root, files);
        }

        public void Dispose()
        {
            files.Dispose();
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestEnvironment(string root, IFileProvider files) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "BigBang1112cz";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = files;
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = files;
    }
}
