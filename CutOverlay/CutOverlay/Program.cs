using ElectronNET.API;
using ElectronNET.API.Entities;

namespace CutOverlay;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseElectron(args);

        builder.Services.AddElectron();
        builder.Services.AddRazorPages();

        var app = builder.Build();

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
        var window = await Electron.WindowManager.CreateWindowAsync(options, "http://localhost:8001/");
        window.SetSize(1920, 1080);

        await app.WaitForShutdownAsync();

        await app.DisposeAsync();
    }
}