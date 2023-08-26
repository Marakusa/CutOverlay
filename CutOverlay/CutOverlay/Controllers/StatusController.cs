using CutOverlay.Services;
using Microsoft.AspNetCore.Mvc;
using CutOverlay.App;
using CutOverlay.Models;
using Newtonsoft.Json;

namespace CutOverlay.Controllers;

[Route("status")]
[ApiController]
public class StatusController : ControllerBase
{
    private readonly Spotify _spotify;
    private readonly BeatSaberPlus _beatSaberPlus;
    private readonly Twitch _twitch;
    private readonly Pulsoid _pulsoid;

    public StatusController(Spotify spotify, BeatSaberPlus beatSaberPlus, Twitch twitch, Pulsoid pulsoid)
    {
        _spotify = spotify;
        _beatSaberPlus = beatSaberPlus;
        _twitch = twitch;
        _pulsoid = pulsoid;
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

    [HttpGet("service/twitch")]
    public IActionResult ServiceTwitch()
    {
        try
        {
            ServiceStatusType status = _twitch.GetStatus();
            ServiceStatusModel statusModel = new()
            {
                Color = status switch
                {
                    ServiceStatusType.Running => "greenyellow",
                    ServiceStatusType.Stopped => "grey",
                    ServiceStatusType.Stopping => "coral",
                    ServiceStatusType.Starting => "darkorange",
                    ServiceStatusType.Error => "crimson",
                    _ => throw new ArgumentOutOfRangeException()
                },
                Status = status switch
                {
                    ServiceStatusType.Running => "Running",
                    ServiceStatusType.Stopped => "Stopped",
                    ServiceStatusType.Stopping => "Stopping",
                    ServiceStatusType.Starting => "Starting",
                    ServiceStatusType.Error => "Error",
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
            return Ok(JsonConvert.SerializeObject(statusModel));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("service/spotify")]
    public IActionResult ServiceSpotify()
    {
        try
        {
            ServiceStatusType status = _spotify.GetStatus();
            ServiceStatusModel statusModel = new()
            {
                Color = status switch
                {
                    ServiceStatusType.Running => "greenyellow",
                    ServiceStatusType.Stopped => "grey",
                    ServiceStatusType.Stopping => "coral",
                    ServiceStatusType.Starting => "darkorange",
                    ServiceStatusType.Error => "crimson",
                    _ => throw new ArgumentOutOfRangeException()
                },
                Status = status switch
                {
                    ServiceStatusType.Running => "Running",
                    ServiceStatusType.Stopped => "Stopped",
                    ServiceStatusType.Stopping => "Stopping",
                    ServiceStatusType.Starting => "Starting",
                    ServiceStatusType.Error => "Error",
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
            return Ok(JsonConvert.SerializeObject(statusModel));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("service/bsp")]
    public IActionResult ServiceBsp()
    {
        try
        {
            ServiceStatusType status = _beatSaberPlus.GetStatus();
            ServiceStatusModel statusModel = new()
            {
                Color = status switch
                {
                    ServiceStatusType.Running => "greenyellow",
                    ServiceStatusType.Stopped => "grey",
                    ServiceStatusType.Stopping => "coral",
                    ServiceStatusType.Starting => "darkorange",
                    ServiceStatusType.Error => "crimson",
                    _ => throw new ArgumentOutOfRangeException()
                },
                Status = status switch
                {
                    ServiceStatusType.Running => "Running",
                    ServiceStatusType.Stopped => "Stopped",
                    ServiceStatusType.Stopping => "Stopping",
                    ServiceStatusType.Starting => "Starting",
                    ServiceStatusType.Error => "Error",
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
            return Ok(JsonConvert.SerializeObject(statusModel));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("service/pulsoid")]
    public IActionResult ServicePulsoid()
    {
        try
        {
            ServiceStatusType status = _pulsoid.GetStatus();
            ServiceStatusModel statusModel = new()
            {
                Color = status switch
                {
                    ServiceStatusType.Running => "greenyellow",
                    ServiceStatusType.Stopped => "grey",
                    ServiceStatusType.Stopping => "coral",
                    ServiceStatusType.Starting => "darkorange",
                    ServiceStatusType.Error => "crimson",
                    _ => throw new ArgumentOutOfRangeException()
                },
                Status = status switch
                {
                    ServiceStatusType.Running => "Running",
                    ServiceStatusType.Stopped => "Stopped",
                    ServiceStatusType.Stopping => "Stopping",
                    ServiceStatusType.Starting => "Starting",
                    ServiceStatusType.Error => "Error",
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
            return Ok(JsonConvert.SerializeObject(statusModel));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}