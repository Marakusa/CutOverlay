using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeZoneNames;

namespace CutOverlay.Pages;

public class OverlayGame1Model : PageModel
{
    private readonly ILogger<OverlayGame1Model> _logger;

    public OverlayGame1Model(ILogger<OverlayGame1Model> logger)
    {
        _logger = logger;
    }
    
    public void OnGet()
    {
    }
}