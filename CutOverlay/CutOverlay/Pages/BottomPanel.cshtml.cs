using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeZoneNames;

namespace CutOverlay.Pages;

public class BottomPanelModel : PageModel
{
    private readonly ILogger<BottomPanelModel> _logger;

    public BottomPanelModel(ILogger<BottomPanelModel> logger)
    {
        _logger = logger;
    }

    public string? Artist { get; set; }
    public string? TimeZoneString { get; set; }

    public void OnGet()
    {
        Artist = "Artist";
        TimeZoneString = ConvertToTimeZoneString(DateTimeOffset.Now);
    }

    public static string ConvertToTimeZoneString(DateTimeOffset dateTimeOffset)
    {
        // Get the timezone abbreviation (short name)
        string timeZoneId = TimeZoneInfo.Local.Id; // example: "Eastern Standard time"
        string lang = CultureInfo.CurrentCulture.Name; // example: "en-US"
        TimeZoneValues abbreviations = TZNames.GetAbbreviationsForTimeZone(timeZoneId, lang);

        // Get the UTC offset
        TimeSpan utcOffset = dateTimeOffset.Offset;

        // Convert the UTC offset to the desired format
        string utcOffsetString = $"{(utcOffset >= TimeSpan.Zero ? "UTC+" : "UTC")}{utcOffset.Hours}";

        // Combine the timezone abbreviation and UTC offset into the final string
        string timeZoneString = $"{abbreviations.Generic} / {utcOffsetString}";

        return timeZoneString;
    }
}