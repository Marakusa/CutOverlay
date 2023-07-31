using Newtonsoft.Json;

namespace CutOverlay.App;

public class CutOverlayApp
{
    public async Task Start()
    {
        Dictionary<string, string?>? configurations = await FetchConfigurationsAsync();

        Spotify spotify = new();
        await spotify.Start(configurations);
        Pulsoid pulsoid = new();
        await pulsoid.Start(configurations);
    }

    private static async Task<Dictionary<string, string?>?> FetchConfigurationsAsync()
    {
        HttpResponseMessage configurationResponse =
            await new HttpClient().GetAsync($"http://localhost:{Globals.Port}/configuration");

        Console.WriteLine("Fetching configurations...");
        string configurationJson = await configurationResponse.Content.ReadAsStringAsync();

        if (configurationResponse.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<Dictionary<string, string?>>(configurationJson);
        Console.WriteLine($"ERROR: {configurationJson}");
        return new Dictionary<string, string?>();
    }
}