namespace CutOverlay.Models;

public class InputModel
{
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
}

public enum Align
{
    Left,
    Center,
    Right,
    Stretch,
    None
}