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
    public int Length { get; set; }
}

public class OverlayStateStatus
{
    public long SongPlayTimestamp { get; set; }
    public bool SongPaused { get; set; }
    public int SongPauseProgress { get; set; }
}

public class OverlayState
{
    public OverlayStateSong? Song { get; set; }
    public OverlayStateStatus? Status { get; set; }
}