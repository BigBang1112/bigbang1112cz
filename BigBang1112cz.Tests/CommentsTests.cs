using BigBang1112cz.Models.Db;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BigBang1112cz.Tests;

// Each test works with a different horn so they can share one app and run in parallel
[ClassDataSource<TestWebApplication>(Shared = SharedType.PerClass)]
public class CommentsTests(TestWebApplication app)
{
    private const string CommentsUrl = "/trackmania/manialink/tmf/bigbang1112/comments";

    [Test]
    public async Task Paging_LinksToNextPageUntilTheLast()
    {
        await SeedCommentsAsync("BAN.ogg", count: 12); // 3 pages of 5

        var client = app.CreateClient();
        var page2 = await client.GetStringAsync($"{CommentsUrl}?horn=BAN.ogg&fromp=1&commentp=2");
        var page3 = await client.GetStringAsync($"{CommentsUrl}?horn=BAN.ogg&fromp=1&commentp=3");

        await Assert.That(page2).Contains("commentp=3");
        await Assert.That(page2).Contains("commentp=1");
        await Assert.That(page3).DoesNotContain("commentp=4");
        await Assert.That(page3).Contains("commentp=2");
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task CommentPageBelowOne_IsRejectedAsClientError(int commentPage)
    {
        var response = await app.CreateClient().GetAsync($"{CommentsUrl}?horn=bird.ogg&fromp=1&commentp={commentPage}");

        await Assert.That((int)response.StatusCode).IsBetween(400, 499);
    }

    [Test]
    public async Task Cache_VariesByFromPageAndLocatorHost()
    {
        var client = app.CreateClient();

        await client.GetStringAsync($"{CommentsUrl}?horn=pan.ogg&fromp=1&commentp=1&locatorhost=BigBang1112cz");
        var second = await client.GetStringAsync($"{CommentsUrl}?horn=pan.ogg&fromp=2&commentp=1&locatorhost=GitHub");

        await Assert.That(second).Contains("p=2&amp;locatorhost=GitHub");
    }

    [Test]
    public async Task PostedComment_ReplacesCachedPage()
    {
        var client = app.CreateGameBoxClient();
        var url = $"{CommentsUrl}?horn=hmm.ogg&fromp=1&commentp=1";

        var before = await client.GetStringAsync(url);
        var post = await client.GetAsync("/trackmania/manialink/tmf/bigbang1112/confirmcomment?horn=hmm.ogg&comment=fresh%20comment&playerlogin=poster&fromp=1");
        var after = await client.GetStringAsync(url);

        await Assert.That(post.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(before).DoesNotContain("fresh comment");
        await Assert.That(after).Contains("fresh comment");
    }

    private Task SeedCommentsAsync(string hornFileName, int count) => app.WithDbAsync(async db =>
    {
        var horn = await db.Horns.SingleAsync(x => x.FileName == hornFileName);
        var user = new HornUserDbModel { Login = $"seeder-{hornFileName}", CreatedAt = DateTimeOffset.UtcNow };

        for (var i = 0; i < count; i++)
        {
            db.HornComments.Add(new HornCommentDbModel
            {
                Horn = horn,
                User = user,
                Content = $"comment {i}",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
            });
        }

        await db.SaveChangesAsync();
    });
}
