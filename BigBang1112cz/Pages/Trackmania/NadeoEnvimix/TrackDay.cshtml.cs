using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BigBang1112cz.Pages.Trackmania.NadeoEnvimix;

public class TrackDayModel : PageModel
{
    public string? Environment { get; set; }
    public string? Difficulty { get; set; }
    public int Map { get; set; }
    public string? Car { get; set; }
    public double Multiplier { get; set; }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        Response.ContentType = "application/xml";
        await next();
    }

    public void OnGet()
    {
        var todaySeed = (int)((DateTimeOffset)DateTime.Today).ToUnixTimeSeconds();
        var rand = new Random(todaySeed);

        Environment = rand.Next(0, 4) switch
        {
            0 => "Canyon",
            1 => "Stadium",
            2 => "Valley",
            3 => "Lagoon",
            _ => "Canyon"
        };

        Difficulty = rand.Next(0, 5) switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            4 => "E",
            _ => "A"
        };

        Map = Difficulty == "E" ? rand.Next(1, 6) : rand.Next(1, 16);

        Car = rand.Next(0, 4) switch
        {
            0 => "CanyonCar",
            1 => "StadiumCar",
            2 => "ValleyCar",
            3 => "LagoonCar",
            _ => "CanyonCar"
        };
    }
}
