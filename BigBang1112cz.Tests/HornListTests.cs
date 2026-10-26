using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BigBang1112cz.Tests;

// The tests change which horns are deleted, so they must not overlap
[ClassDataSource<TestWebApplication>(Shared = SharedType.PerClass)]
[NotInParallel]
public partial class HornListTests(TestWebApplication app)
{
    private const string ListUrl = "/trackmania/manialink/tmf/bigbang1112";

    [Test]
    public async Task PageCount_IgnoresDeletedHorns()
    {
        await KeepOnlyAsync(3);

        var response = await app.CreateClient().GetStringAsync($"{ListUrl}?p=1");

        await Assert.That(response).Contains("$o1/1");
    }

    [Test]
    public async Task EmptyCollection_StillRenders()
    {
        await KeepOnlyAsync(0);

        var response = await app.CreateClient().GetAsync($"{ListUrl}?p=1");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("$o1/1");
    }

    [Test]
    public async Task Bulk_OnlyPicksExistingHorns()
    {
        var kept = await KeepOnlyAsync(3);

        var response = await app.CreateGameBoxClient().GetStringAsync("/trackmania/manialink/tmf/bigbang1112/bulk?playerlogin=bulker&fromp=1");
        var installed = InstallSkinName().Matches(response).Select(x => x.Groups[1].Value).ToList();

        await Assert.That(installed).IsEquivalentTo(kept);
    }

    /// <summary>
    /// Marks all but the first <paramref name="count"/> horns as deleted and returns the kept file names.
    /// </summary>
    private async Task<List<string>> KeepOnlyAsync(int count)
    {
        var kept = await app.WithDbAsync(async db =>
        {
            var horns = await db.Horns.OrderBy(x => x.FileName).ToListAsync();

            for (var i = 0; i < horns.Count; i++)
            {
                horns[i].IsDeleted = i >= count;
            }

            await db.SaveChangesAsync();

            return horns.Take(count).Select(x => x.FileName).ToList();
        });

        await app.EvictCacheAsync("comments", "downloads");

        return kept;
    }

    [GeneratedRegex("<install_skin>\\s*<name>([^<]+)</name>")]
    private static partial Regex InstallSkinName();
}
