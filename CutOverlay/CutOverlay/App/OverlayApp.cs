namespace CutOverlay.App;

public abstract class OverlayApp : IDisposable
{
    private protected HttpClient? HttpClient = null;
    private protected ServiceStatusType Status = ServiceStatusType.Stopped;

    public void Dispose()
    {
        Unload();
    }

    public abstract void Unload();

    public abstract ServiceStatusType GetStatus();
}

public enum ServiceStatusType
{
    Running, Starting, Stopping, Stopped, Error
}