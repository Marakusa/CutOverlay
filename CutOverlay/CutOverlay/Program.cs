using ElectronNET.API;
using ElectronNET.API.Entities;

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
            name: "default",
            pattern: "{controller=Index}");

        await app.StartAsync();

        BrowserWindowOptions options = new()
        {
            Title = "CUT Overlay",
            Closable = true,
            BackgroundColor = "#00000000",
            AlwaysOnTop = false,
            AutoHideMenuBar = true,
            Center = false,
            Width = 1700,
            Height = 800,
            MinWidth = 750,
            MinHeight = 650,
            Resizable = true
        };

        // Open the Electron-Window here
        await Electron.WindowManager.CreateWindowAsync(options, "http://localhost:8001/");

        CutOverlayApp overlay = new();
        overlay.StartAsync();

        await app.WaitForShutdownAsync();

        await app.DisposeAsync();
    }
}