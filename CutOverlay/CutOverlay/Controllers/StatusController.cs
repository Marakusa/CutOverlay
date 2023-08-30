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
    private readonly LoggerService _logger;

    public StatusController(Spotify spotify, BeatSaberPlus beatSaberPlus, Twitch twitch, Pulsoid pulsoid, LoggerService logger)
    {
        _spotify = spotify;
        _beatSaberPlus = beatSaberPlus;
        _twitch = twitch;
        _pulsoid = pulsoid;
        _logger = logger;
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

    private IActionResult StatusCheck(ServiceStatusType status)
    {
        ServiceStatusModel statusModel = new()
        {
            Color = status switch
            {
                ServiceStatusType.Running => "greenyellow",
                ServiceStatusType.Stopped => "grey",
                ServiceStatusType.Waiting => "grey",
                ServiceStatusType.Stopping => "coral",
                ServiceStatusType.Starting => "darkorange",
                ServiceStatusType.Error => "crimson",
                _ => throw new ArgumentOutOfRangeException()
            },
            Status = status switch
            {
                ServiceStatusType.Running => "Running",
                ServiceStatusType.Stopped => "Stopped",
                ServiceStatusType.Waiting => "Waiting",
                ServiceStatusType.Stopping => "Stopping",
                ServiceStatusType.Starting => "Starting",
                ServiceStatusType.Error => "Error",
                _ => throw new ArgumentOutOfRangeException()
            }
        };
        return Ok(JsonConvert.SerializeObject(statusModel));
    }

    [HttpGet("service/twitch")]
    public IActionResult ServiceTwitch()
    {
        try
        {
            ServiceStatusType status = _twitch.GetStatus();
            return StatusCheck(status);
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
            return StatusCheck(status);
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
            return StatusCheck(status);
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
            return StatusCheck(status);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("service/logger")]
    public IActionResult ServiceLogger()
    {
        try
        {
            ServiceStatusType status = _logger.GetStatus();
            return StatusCheck(status);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("service/logger/test")]
    public IActionResult ServiceLoggerTest()
    {
        try
        {
            _logger.LogTrace("Test message");
            _logger.LogDebug("Test message");
            _logger.LogInformation("Test message");
            _logger.LogWarning("Test message");
            _logger.LogError("Test message");
            _logger.LogCritical("Test message");
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}