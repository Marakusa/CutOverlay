using System.Reflection;
using CutOverlay.App.Overlay;
using Newtonsoft.Json;

namespace CutOverlay.App;

public class CutOverlay
{
    public async Task<OverlayApp[]> Start()
    {
        _ = new Status();

        Dictionary<string, string?>? configurations = await FetchConfigurationsAsync();

        // Get all classes with the Overlay attribute
        IEnumerable<Type> overlays = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => type.GetCustomAttributes(typeof(OverlayAttribute), true).Length > 0);

        List<OverlayApp> overlayApps = new();

        // Start the overlay apps
        foreach (Type overlay in overlays)
            try
            {
                OverlayApp instance = (OverlayApp)Activator.CreateInstance(overlay)!;
                _ = instance.Start(configurations);
                overlayApps.Add(instance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start {overlay.FullName} app: {ex}");
            }

        return overlayApps.ToArray();
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