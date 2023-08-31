using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeZoneNames;

namespace CutOverlay.Pages;

public class OverlayStartingModel : PageModel
{
    private readonly ILogger<OverlayStartingModel> _logger;

    public OverlayStartingModel(ILogger<OverlayStartingModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}