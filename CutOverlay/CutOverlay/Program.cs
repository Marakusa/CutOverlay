using CutOverlay.App.Overlay;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Newtonsoft.Json;

namespace CutOverlay;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseElectron(args);

        builder.Services.AddElectron();
        builder.Services.AddRazorPages();

        WebApplication app = builder.Build();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.MapRazorPages();

        app.MapControllerRoute(
            "default",
            "{controller=Index}");

        await app.StartAsync();

        BrowserWindowOptions options = new()
        {
            Title = "CUT Overlay",
            Closable = true,
            BackgroundColor = "#00000000",
            AlwaysOnTop = false,
            AutoHideMenuBar = true,
            Center = false,
            Width = 1100,
            Height = 800,
            MinWidth = 750,
            MinHeight = 650,
            Resizable = true
        };

        string dataFolder = $"{AppContext.BaseDirectory}electron.manifest.json";
        string content = await File.ReadAllTextAsync(dataFolder);
        var manifest = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
        Globals.Port = int.Parse(manifest?["aspCoreBackendPort"].ToString() ?? "0");

        // Open the Electron-Window here
        BrowserWindow? window =
            await Electron.WindowManager.CreateWindowAsync(options, $"http://localhost:{Globals.Port}/");

        App.CutOverlay cutOverlay = new();
        OverlayApp[] overlays = await cutOverlay.Start();

        window.OnClose += () =>
        {
            foreach (OverlayApp overlay in overlays)
            {
                OverlayApp? instance = overlay.GetInstance();
                instance?.Unload();
            }
        };

        await app.WaitForShutdownAsync();

        await app.DisposeAsync();
    }
}