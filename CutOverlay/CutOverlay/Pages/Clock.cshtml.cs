using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using TimeZoneNames;

namespace CutOverlay.Pages;

public class ClockModel : PageModel
{
    public string? TimeZoneString { get; set; }

    public void OnGet()
    {
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