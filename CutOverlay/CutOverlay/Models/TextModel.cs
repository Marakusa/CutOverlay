namespace CutOverlay.Models;

public class TextModel
{
    public string? Text { get; set; }
    public string? StyleId { get; set; }
    public string StyleIdDiv => StyleId + "Div";
    public string? StyleIdText => StyleId;
    public Align Align { get; set; }
    public int FontSize { get; set; } = 36;

    public string AlignmentStyle =>
        Align switch
        {
            Align.Left => "left",
            Align.None => "left",
            Align.Center => "center",
            Align.Stretch => "center",
            Align.Right => "right",
            _ => "left"
        };
}