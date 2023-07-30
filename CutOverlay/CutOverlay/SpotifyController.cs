using Microsoft.AspNetCore.Mvc;

namespace CutOverlay;

[Route("spotify")]
[ApiController]
public class SpotifyController : ControllerBase
{
    [HttpGet("callback")]
    public IActionResult SpotifyCallback([FromQuery(Name = "code")] string accessToken)
    {
        try
        {
            if (Spotify.Instance != null)
            {
                Spotify.Instance.AccessToken = accessToken;
                _ = Spotify.Instance.UpdateAuthorizationAsync();
            }

            return Ok("Authorization successful! This tab can now be closed.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
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