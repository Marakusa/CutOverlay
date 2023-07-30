namespace CutOverlay;

public class CutOverlayApp
{
    public void Start()
    {
        Spotify spotify = new();
        _ = spotify.Start();
    }
}