using Microsoft.AspNetCore.Mvc;

namespace CutOverlay;

[Route("spotify")]
[ApiController]
public class SpotifyStatusController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status()
    {
        try
        {
            string configPath = $"{AppContext.BaseDirectory}data\\status.json";
            return Ok(System.IO.File.Exists(configPath) ? System.IO.File.ReadAllText(configPath) : "{}");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}