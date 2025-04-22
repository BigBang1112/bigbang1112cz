using BigBang1112cz.Models.Trackmania.Manialink;

namespace BigBang1112cz.Services;

public static class HornLocatorHost
{
    public static string GetUrl(HttpRequest request, HostType type, string fileName) => type switch
    {
        HostType.BigBang1112cz => $"https://{request.Host}/horns/{fileName}",
        HostType.GitHub => $"https://raw.githubusercontent.com/BigBang1112/bigbang1112cz/refs/heads/main/BigBang1112cz/wwwroot/horns/{fileName}",
        HostType.Dashmap => $"https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/{fileName}",
        HostType.ManiaCDN => $"http://maniacdn.net/bigbang1112/horns/{fileName}",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
