using BigBang1112cz.Models.Trackmania.Manialink;
using BigBang1112cz.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace BigBang1112cz.Pages.Shared;

/// <summary>
/// Base for TMF manialink pages opened by a player in-game via <c>addplayerid="1"</c>.
/// Short-circuits in manialink registration mode and rejects requests not coming from the game client.
/// </summary>
public abstract class TmfPlayerPageModel : XmlPageModel
{
    private readonly IOptions<TrackmaniaOptions> options;
    private readonly ILogger logger;

    [FromQuery(Name = "fromp")]
    public int FromPageNum { get; set; }

    [FromQuery]
    [StringLength(255)]
    public required string? Nickname { get; set; }

    [FromQuery(Name = "playerlogin")]
    [StringLength(255, MinimumLength = 2)]
    public required string Login { get; set; }

    [FromQuery(Name = "path")]
    [StringLength(255)]
    public required string? Zone { get; set; }

    [FromQuery]
    public HostType LocatorHost { get; set; }

    /// <summary>
    /// Nickname without TM formatting codes, for logging.
    /// </summary>
    protected string? DeformattedNickname { get; private set; }

    protected TmfPlayerPageModel(IOptions<TrackmaniaOptions> options, IHostEnvironment env, ILogger logger) : base(env)
    {
        this.options = options;
        this.logger = logger;
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        base.OnPageHandlerExecuting(context);

        if (options.Value.ManialinkRegistrationMode)
        {
            context.Result = StatusCode(200);
            return;
        }

        DeformattedNickname = Nickname is null ? null : TextFormatter.Deformat(Nickname, maxReplacementCount: 1000);

        if (!Request.Headers.UserAgent.Equals("GameBox"))
        {
            logger.LogWarning("Request to {Path} by {Nickname} (login: {Login}) from non-GameBox client", Request.Path, DeformattedNickname, Login);
            context.Result = BadRequest();
        }
    }
}
