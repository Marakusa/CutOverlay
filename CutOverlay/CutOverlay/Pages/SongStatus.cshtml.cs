using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CutOverlay.Pages;

public class SongStatusModel : PageModel
{
    private readonly ILogger<SongStatusModel> _logger;

    public SongStatusModel(ILogger<SongStatusModel> logger)
    {
        _logger = logger;
    }
    
    public void OnGet()
    {
    }
}