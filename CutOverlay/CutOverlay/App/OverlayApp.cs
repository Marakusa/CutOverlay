using Newtonsoft.Json;

namespace CutOverlay.App;

public class OverlayApp : IDisposable
{
    private protected readonly HttpClient? HttpClient;

    public OverlayApp()
    {
        HttpClient = new HttpClient();
    }

    public void Dispose()
    {
        Unload();
    }

    public virtual Task Start(Dictionary<string, string?>? configurations)
    {
        return Task.CompletedTask;
    }

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