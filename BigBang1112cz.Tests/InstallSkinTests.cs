using BigBang1112cz.Pages.Trackmania.Manialink;

namespace BigBang1112cz.Tests;

public class InstallSkinFileNameTests
{
    [Test]
    [Arguments("skin.zip", "skin.zip")]
    [Arguments("\"skin.zip\"", "skin.zip")]
    [Arguments("  \"skin.zip\"  ", "skin.zip")]
    [Arguments("/files/skins/skin.zip", "skin.zip")]
    [Arguments("../../evil.zip", "evil.zip")]
    [Arguments("..\\..\\evil.zip", "evil.zip")]
    [Arguments("C:\\Windows\\evil.zip", "evil.zip")]
    [Arguments("folder/", null)]
    [Arguments("..", null)]
    [Arguments(".", null)]
    [Arguments("   ", null)]
    [Arguments(null, null)]
    public async Task SanitizeFileName_StripsQuotesAndDirectories(string? input, string? expected)
    {
        await Assert.That(InstallSkinModel.SanitizeFileName(input)).IsEqualTo(expected);
    }
}

[ClassDataSource<TestWebApplication>(Shared = SharedType.PerClass)]
public class InstallSkinTests(TestWebApplication app)
{
    [Test]
    public async Task LocalUrl_IsNotRequested()
    {
        var response = await app.CreateClient().GetStringAsync("/trackmania/manialink/installskin?type=Horns&url=http://127.0.0.1:1/skin.zip");

        await Assert.That(response).Contains("URL could not be reached");
    }

    [Test]
    public async Task Cache_VariesByUrl()
    {
        var client = app.CreateClient();

        var invalid = await client.GetStringAsync("/trackmania/manialink/installskin?type=Avatars&url=ftp://example.com/skin.zip");
        var unreachable = await client.GetStringAsync("/trackmania/manialink/installskin?type=Avatars&url=http://127.0.0.1:1/skin.zip");

        await Assert.That(invalid).Contains("URL is not valid");
        await Assert.That(unreachable).Contains("URL could not be reached");
    }
}
