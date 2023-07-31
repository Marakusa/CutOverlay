using Microsoft.AspNetCore.Mvc;

namespace CutOverlay.Controllers;

[Route("pulsoid")]
[ApiController]
public class PulsoidController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status()
    {
        try
        {
            string path = $"{Globals.GetAppDataPath()}data\\pulsoid.txt";
            return Ok(System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}