using CutOverlay.Services;
using Microsoft.AspNetCore.Mvc;

namespace CutOverlay.Controllers;

[Route("spotify")]
[ApiController]
public class SpotifyController : ControllerBase
{
    private readonly Spotify _spotify;

    public SpotifyController(Spotify spotify)
    {
        _spotify = spotify;
    }

    [HttpGet("callback")]
    public IActionResult SpotifyCallback([FromQuery(Name = "code")] string accessToken,
        [FromQuery(Name = "state")] string state)
    {
        try
        {
            _spotify.AuthCallback(accessToken, state);

            return new ContentResult
            {
                Content =
                    "<div>Authorization successful! This tab can now be closed.</div><script>setTimeout(() => {close();},3000);</script>",
                ContentType = "text/html",
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            return new ContentResult
            {
                Content = $"<div>{ex.Message}</div>",
                ContentType = "text/html",
                StatusCode = 400
            };
        }
    }
}