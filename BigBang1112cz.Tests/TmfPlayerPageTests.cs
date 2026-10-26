using BigBang1112cz.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace BigBang1112cz.Tests;

[ClassDataSource<TestWebApplication>(Shared = SharedType.PerClass)]
public class TmfPlayerPageTests(TestWebApplication app)
{
    private const string BaseUrl = "/trackmania/manialink/tmf/bigbang1112";

    [Test]
    [Arguments("bulk?playerlogin=someone")]
    [Arguments("confirmdownload?horn=BAN.ogg&playerlogin=someone")]
    [Arguments("confirmcomment?horn=BAN.ogg&comment=hi&playerlogin=someone")]
    [Arguments("addcomment?horn=BAN.ogg&comment=hi&playerlogin=someone")]
    [Arguments("download?horn=BAN.ogg&playerlogin=someone")]
    public async Task NonGameBoxClient_IsRejected(string pathAndQuery)
    {
        var response = await app.CreateClient().GetAsync($"{BaseUrl}/{pathAndQuery}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task NicknameChange_IsSavedWithHistory()
    {
        var client = app.CreateGameBoxClient();

        await client.GetAsync($"{BaseUrl}/confirmdownload?horn=fa.ogg&playerlogin=renamer&nickname=OldName&fromp=1");
        await client.GetAsync($"{BaseUrl}/confirmdownload?horn=fa.ogg&playerlogin=renamer&nickname=NewName&fromp=1");

        var user = await app.WithDbAsync(db => db.HornUsers
            .Include(x => x.History)
            .SingleAsync(x => x.Login == "renamer"));

        await Assert.That(user.Nickname).IsEqualTo("NewName");
        await Assert.That(user.History).HasSingleItem();
        await Assert.That(user.History.Single().Nickname).IsEqualTo("OldName");
    }

    [Test]
    public async Task TooLongComment_RedirectsBackWithMessage()
    {
        var comment = new string('x', 97);

        var response = await app.CreateGameBoxClient().GetStringAsync($"{BaseUrl}/addcomment?horn=BAN.ogg&comment={comment}&playerlogin=someone&fromp=1");

        await Assert.That(response).Contains("maximum length of 96");
        await Assert.That(response).Contains("/comments?horn=BAN.ogg");
    }
}

public class RegistrationModeWebApplication : TestWebApplication
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.PostConfigure<TrackmaniaOptions>(options => options.ManialinkRegistrationMode = true);
    }
}

[ClassDataSource<RegistrationModeWebApplication>(Shared = SharedType.PerClass)]
public class RegistrationModeTests(RegistrationModeWebApplication app)
{
    [Test]
    public async Task PlayerPage_ReturnsEmptyOkForAnyClient()
    {
        // Not a GameBox client and no player parameters, which would otherwise be rejected
        var response = await app.CreateClient().GetAsync("/trackmania/manialink/tmf/bigbang1112/bulk");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await response.Content.ReadAsStringAsync()).IsEmpty();
    }
}
