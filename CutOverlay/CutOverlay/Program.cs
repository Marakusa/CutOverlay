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

        await app.StartAsync();

        BrowserWindowOptions options = new()
        {
            Title = "CUT Overlay",
            Closable = true,
            BackgroundColor = "#00000000",
            AlwaysOnTop = false,
            AutoHideMenuBar = true,
            Center = false,
            Width = 1920,
            Height = 1080,
            Resizable = false
        };

        // Open the Electron-Window here
        BrowserWindow? window = await Electron.WindowManager.CreateWindowAsync(options, "http://localhost:8001/");
        window.SetSize(1920, 1080);

        //CutOverlayApp overlay = new();
        //overlay.StartAsync();

        await app.WaitForShutdownAsync();

        await app.DisposeAsync();
    }
}