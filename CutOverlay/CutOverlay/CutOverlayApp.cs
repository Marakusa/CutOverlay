using CutOverlay.Models;
using Newtonsoft.Json;

namespace CutOverlay;

public class CutOverlayApp
{
    public async Task StartAsync()
    {
        OverlayState test = new()
        {
            Song = new OverlayStateSong
            {
                Artist = "Test artist",
                Name = "Song",
                Length = 120,
                Color = new OverlayStateSongColor
                {
                    Red = 0.8,
                    Green = 0.2,
                    Blue = 0.6
                }
            },
            Status = new OverlayStateStatus
            {
                SongPlayTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                SongPaused = true,
                SongPauseProgress = 10
            }
        };

        if (!Directory.Exists($"{AppContext.BaseDirectory}data"))
            Directory.CreateDirectory($"{AppContext.BaseDirectory}data");

        Console.WriteLine($"{AppContext.BaseDirectory}data\\status.json");
        await File.WriteAllTextAsync($"{AppContext.BaseDirectory}data\\status.json", JsonConvert.SerializeObject(test));
    }
}