using System.Globalization;
using CutOverlay.App.Overlay;
using CutOverlay.Models.Twitch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CutOverlay.Controllers;

[Route("twitch")]
[ApiController]
public class TwitchController : ControllerBase
{
    [HttpGet("callback")]
    public IActionResult TwitchCallback()
    {
        return new ContentResult
        {
            Content = "<script>window.location = window.location.origin + \"/twitch/callback/query?\" + window.location.hash.substring(1);</script>",
            ContentType = "text/html"
        };
    }

    [HttpGet("callback/query")]
    public async Task<IActionResult> SpotifyCallback([FromQuery(Name = "access_token")] string? accessToken,
        [FromQuery(Name = "state")] string state)
    {
        try
        {
            if (Twitch.Instance == null)
                throw new Exception("Twitch app was not started");

            await Twitch.Instance.SetOAuth(accessToken, state);

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
                Content = $"<div>{ex}</div><br><div>{ex.StackTrace}</div>",
                ContentType = "text/html",
                StatusCode = 400
            };
        }
    }

    [HttpGet("followers")]
    public IActionResult SpotifyCallback([FromQuery(Name = "since")] string? since)
    {
        try
        {
            if (Twitch.Instance == null)
                throw new Exception("Twitch app was not started");

            NewFollowerData followers = Twitch.Instance.GetFollowers(string.IsNullOrEmpty(since)
                ? DateTime.UnixEpoch
                : DateTime.ParseExact(since, "yyyy-MM-dd'T'HH.mm.ss'Z'", CultureInfo.InvariantCulture));

            return Ok(JsonConvert.SerializeObject(followers));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}