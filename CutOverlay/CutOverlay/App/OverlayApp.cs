namespace CutOverlay.App;

public abstract class OverlayApp : IDisposable
{
    private protected HttpClient? HttpClient = null;

    public void Dispose()
    {
        Unload();
    }

    public abstract void Unload();
}