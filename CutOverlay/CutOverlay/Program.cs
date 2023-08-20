using CutOverlay.Services;
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

        builder.Services.AddSingleton<ConfigurationService>();
        builder.Services.AddSingleton<OverlayStatusService>(); 
        builder.Services.AddSingleton<Spotify>();
        builder.Services.AddSingleton<Twitch>();
        builder.Services.AddSingleton<Pulsoid>();
        builder.Services.AddSingleton<BeatSaberPlus>();

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
        Globals.ChatWebSocketPort = 37101;

        // Open the Electron-Window here
        await Electron.WindowManager.CreateWindowAsync(options, $"http://localhost:{Globals.Port}/");
        
        await app.WaitForShutdownAsync();

        await app.DisposeAsync();
    }
}