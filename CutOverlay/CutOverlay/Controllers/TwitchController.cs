using System.Globalization;
using CutOverlay.Models.Twitch;
using CutOverlay.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CutOverlay.Controllers;

[Route("twitch")]
[ApiController]
public class TwitchController : ControllerBase
{
    private readonly Twitch _twitch;

    public TwitchController(Twitch twitch)
    {
        _twitch = twitch;
    }

    [HttpGet("apiConnection")]
    public IActionResult ApiConnectionCallback()
    {
        try
        {
            _ = _twitch;
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("callback")]
    public IActionResult TwitchCallback()
    {
        return new ContentResult
        {
            Content =
                "<script>window.location = window.location.origin + \"/twitch/callback/query?\" + window.location.hash.substring(1);</script>",
            ContentType = "text/html"
        };
    }

    [HttpGet("callback/query")]
    public async Task<IActionResult> TwitchCallbackQuery([FromQuery(Name = "access_token")] string? accessToken,
        [FromQuery(Name = "state")] string state)
    {
        try
        {
            await _twitch.SetOAuth(accessToken, state);

            return new ContentResult
            {
                Content =
                    "<div>Authorization successful! This tab can now be closed.</div><script>setTimeout(() => {close();},100);</script>",
                ContentType = "text/html",
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            return new ContentResult
            {
                Content = $"<div>{ex}</div><br><div>{ex.StackTrace}</div>",
                ContentType = "text/html",
                StatusCode = 400
            };
        }
    }

    [HttpGet("followers")]
    public IActionResult FollowersCallback([FromQuery(Name = "since")] string? since)
    {
        try
        {
            NewFollowerData followers = _twitch.GetFollowers(string.IsNullOrEmpty(since)
                ? DateTime.UnixEpoch
                : DateTime.ParseExact(since, "yyyy-MM-dd'T'HH.mm.ss'Z'", CultureInfo.InvariantCulture));

            return Ok(JsonConvert.SerializeObject(followers));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("latest/follow")]
    public async Task<IActionResult> LatestFollowCallback()
    {
        try
        {
            return Ok($"{{\"name\":\"{await _twitch.GetLatestFollowerAsync()}\"}}");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("latest/sub")]
    public async Task<IActionResult> LatestSubCallback()
    {
        try
        {
            return Ok($"{{\"name\":\"{await _twitch.GetLatestSubscriberAsync()}\"}}");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}