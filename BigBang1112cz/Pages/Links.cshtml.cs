using BigBang1112cz.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace BigBang1112cz.Pages;

[OutputCache(Duration = 3600)]
public class LinksModel : PageModel
{
    public record Link(string Label, string Url);

    public static readonly IReadOnlyList<Link> Links =
    [
        new("GitHub", "https://github.com/BigBang1112"),
        new("Ko-fi", "https://ko-fi.com/BigBang1112"),
        new("PayPal", "https://paypal.me/BigBang1112"),
        new("X — BigBang1112tm", "https://x.com/BigBang1112tm"),
        new("X — BigBang1112", "https://x.com/BigBang1112real"),
        new("YouTube — BigBang1112tm", "https://youtube.com/@BigBang1112tm"),
        new("YouTube — BigBang1112", "https://youtube.com/@BigBang1112"),
        new("YouTube — BigBang1112dev", "https://youtube.com/@BigBang1112dev"),
        new("YouTube — BigBang1112tf", "https://youtube.com/@BigBang1112tf"),
        new("YouTube — Realnest", "https://youtube.com/@realnestmusic"),
        new("SoundCloud — Realnest", "https://soundcloud.com/realnest"),
        new("Instagram — Realnest", "https://instagram.com/realnestmusic"),
        new("Spotify — Realnest", "https://open.spotify.com/artist/4hCfEugmmJX2po152bsceI"),
        new("Discord — GameBox Sandbox", "https://discord.gbx.tools"),
        new("Discord — Nations Converter","https://discord.nc.gbx.tools"),
        new("Discord — Nadeo Envimix", "https://discord.envimix.gbx.tools"),
        new("Discord — BigBang1112tm", "https://discord.gg/q9whS3c"),
        new("Facebook — BigBang1112tm", "https://facebook.com/BigBang1112tm"),
    ];

    public void OnGet()
    {
        Response.ClientCache();
    }
}
