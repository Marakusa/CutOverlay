using Newtonsoft.Json;

namespace CutOverlay.App;

public abstract class OverlayApp : IDisposable
{
    private protected HttpClient? HttpClient = null;

    public void Dispose()
    {
        Unload();
    }
    
    public abstract Task Start(Dictionary<string, string?>? configurations);

    private protected async Task<Dictionary<string, string?>?> FetchConfigurationsAsync()
    {
        HttpResponseMessage configurationResponse =
            await HttpClient!.GetAsync($"http://localhost:{Globals.Port}/configuration");

        Console.WriteLine("Fetching configurations...");
        string configurationJson = await configurationResponse.Content.ReadAsStringAsync();

        if (configurationResponse.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<Dictionary<string, string?>>(configurationJson);
        Console.WriteLine($"ERROR: {configurationJson}");
        return new Dictionary<string, string?>();
    }

    public virtual void Unload()
    {
        throw new NotImplementedException();
    }
}