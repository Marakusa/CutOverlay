using CutOverlay.App.Overlay;
using Microsoft.AspNetCore.Mvc;

namespace CutOverlay.Controllers;

[Route("spotify")]
[ApiController]
public class SpotifyController : ControllerBase
{
    [HttpGet("callback")]
    public IActionResult SpotifyCallback([FromQuery(Name = "code")] string accessToken,
        [FromQuery(Name = "state")] string state)
    {
        try
        {
            if (Spotify.Instance == null)
                throw new Exception("Spotify app was not started");

            Spotify.Instance.AuthCallback(accessToken, state);

            return new ContentResult
            {
                Content = "<div>Authorization successful! This tab can now be closed.</div><script>setTimeout(() => {close();},3000);</script>",
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

    [HttpGet("status")]
    public IActionResult Status()
    {
        try
        {
            string configPath = $"{Globals.GetAppDataPath()}data\\status.json";
            return Ok(System.IO.File.Exists(configPath) ? System.IO.File.ReadAllText(configPath) : "{}");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("image")]
    public IActionResult Image()
    {
        try
        {
            string artworkPath = $"{Globals.GetAppDataPath()}data\\cover.jpg";
            return File(System.IO.File.ReadAllBytes(artworkPath), "image/jpeg");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}