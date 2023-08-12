namespace CutOverlay;

public static class Globals
{
    public static int Port { get; set; }
    public static int ChatWebSocketPort { get; set; }

    public static string GetAppDataPath()
    {
        string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "cut-overlay");
        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        return $"{appDataPath}\\";
    }
}