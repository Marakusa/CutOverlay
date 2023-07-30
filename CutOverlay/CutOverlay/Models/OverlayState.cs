namespace CutOverlay.Models;

public class OverlayStateSongColor
{
    public double Red { get; set; }
    public double Green { get; set; }
    public double Blue { get; set; }
}

public class OverlayStateSong
{
    public string? Artist { get; set; }
    public string? Name { get; set; }
    public OverlayStateSongColor? Color { get; set; }
}

public class OverlayStateStatus
{
    public long FetchTime { get; set; }
    public long Total { get; set; }
    public long Progress { get; set; }
    public bool Paused { get; set; }
}

public class OverlayState
{
    public OverlayStateSong? Song { get; set; }
    public OverlayStateStatus? Status { get; set; }
}