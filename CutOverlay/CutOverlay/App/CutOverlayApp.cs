using Newtonsoft.Json;
using System.Reflection;

namespace CutOverlay.App;

public class CutOverlayApp
{
    public async Task Start()
    {
        _ = new StatusApp();

        Dictionary<string, string?>? configurations = await FetchConfigurationsAsync();

        // Get all classes with the Overlay attribute
        IEnumerable<Type> overlays = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => type.GetCustomAttributes(typeof(OverlayAttribute), true).Length > 0);

        // Start the overlay apps
        foreach (Type overlay in overlays)
        {
            OverlayApp instance = (OverlayApp)Activator.CreateInstance(overlay)!;
            _ = instance.Start(configurations);
        }
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