using System.Web;

namespace CutOverlay.Models;

public class InputModelObs
{
    public string? ObsSourceName { get; set; }
    public int? ObsSourceWidth { get; set; }
    public int? ObsSourceHeight { get; set; }
    public string? Icon { get; set; }
    public string? Label { get; set; }
    public string? Id { get; set; }
    public string? Value { get; set; }
    public string? OnClick { get; set; }
    public Align AlignContent { get; set; } = Align.Left;

    public string AlignmentClass =>
        AlignContent switch
        {
            Align.Left => "buttonLeft",
            Align.Center => "buttonCenter",
            Align.Right => "buttonRight",
            Align.Stretch => "buttonStretch",
            Align.None => "button",
            _ => "buttonLeft"
        };

    public string GetSourceQuery()
    {
        List<string> queryItems = new();
        if (!string.IsNullOrEmpty(ObsSourceName))
            queryItems.Add($"layer-name={HttpUtility.UrlEncode(ObsSourceName)}");
        if (ObsSourceWidth != null)
            queryItems.Add($"layer-width={HttpUtility.UrlEncode(ObsSourceWidth.ToString())}");
        if (ObsSourceHeight != null)
            queryItems.Add($"layer-height={HttpUtility.UrlEncode(ObsSourceHeight.ToString())}");
        return string.Join('&', queryItems);
    }
}