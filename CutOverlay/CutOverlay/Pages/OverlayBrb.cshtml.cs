using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeZoneNames;

namespace CutOverlay.Pages;

public class OverlayBrbModel : PageModel
{
    private readonly ILogger<OverlayBrbModel> _logger;

    public OverlayBrbModel(ILogger<OverlayBrbModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}