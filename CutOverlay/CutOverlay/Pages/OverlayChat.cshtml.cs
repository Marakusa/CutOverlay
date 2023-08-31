using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CutOverlay.Pages;

public class OverlayChatModel : PageModel
{
    private readonly ILogger<OverlayChatModel> _logger;

    public OverlayChatModel(ILogger<OverlayChatModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}