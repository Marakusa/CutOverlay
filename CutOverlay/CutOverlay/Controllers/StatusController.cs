using CutOverlay.Services;
using Microsoft.AspNetCore.Mvc;

namespace CutOverlay.Controllers;

[Route("status")]
[ApiController]
public class StatusController : ControllerBase
{
    public StatusController(Spotify spotify, BeatSaberPlus beatSaberPlus)
    {
        _ = spotify;
        _ = beatSaberPlus;
    }

    [HttpGet("get")]
    public IActionResult Get()
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