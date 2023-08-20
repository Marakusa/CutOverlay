using CutOverlay.Services;
using Microsoft.AspNetCore.Mvc;

namespace CutOverlay.Controllers;

[Route("pulsoid")]
[ApiController]
public class PulsoidController : ControllerBase
{
    private readonly Pulsoid _pulsoid;

    public PulsoidController(Pulsoid pulsoid)
    {
        _pulsoid = pulsoid;
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        try
        {
            return Ok(_pulsoid.GetHeartBeat());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}