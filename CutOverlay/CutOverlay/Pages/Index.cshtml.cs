using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CutOverlay.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public string CopyrightString => "CUT Overlay (c) 2023 Markus Kannisto";

    public string? Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString();

    public string LicenseInformation =>
        "This application is licensed under the GNU General Public License v3.0 (GPL-3.0)";

    public void OnGet()
    {
    }
}