using Newtonsoft.Json;

namespace CutOverlay.App.Overlay;

public abstract class OverlayApp : IDisposable
{
    private protected HttpClient? HttpClient = null;
    
    public void Dispose()
    {
        Unload();
    }

    public abstract OverlayApp? GetInstance();

    public abstract Task Start(Dictionary<string, string?>? configurations);

    public abstract void Unload();

    private protected async Task<Dictionary<string, string?>?> FetchConfigurationsAsync()
    {
        HttpResponseMessage configurationResponse =
            await HttpClient!.GetAsync($"http://localhost:{Globals.Port}/configuration");

        string configurationJson = await configurationResponse.Content.ReadAsStringAsync();

        if (configurationResponse.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<Dictionary<string, string?>>(configurationJson);
        Console.WriteLine($"Failed to fetch configurations: {configurationJson}");
        return new Dictionary<string, string?>();
    }
}