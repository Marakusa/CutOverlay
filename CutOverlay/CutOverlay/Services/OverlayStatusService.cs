using ColorThief;
using CutOverlay.Models;
using Newtonsoft.Json;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace CutOverlay.Services;

public class OverlayStatusService : IDisposable
{
    // http://garethrees.org/2007/11/14/pngcrush/
    private readonly byte[] _blankImage =
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    };

    private readonly Dictionary<Type, PlaybackStateQueue> _currentPlaybacks = new();
    private readonly HttpClient? _httpClient;

    public OverlayStatusService()
    {
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    public async Task SaveStateAsync<T>(PlaybackState? playbackState, int priority)
    {
        if (_currentPlaybacks.ContainsKey(typeof(T)))
        {
            _currentPlaybacks[typeof(T)].PlaybackState = playbackState;
            _currentPlaybacks[typeof(T)].Priority = priority;
        }
        else
        {
            _currentPlaybacks.Add(typeof(T), new PlaybackStateQueue(playbackState, priority));
        }

        KeyValuePair<Type, PlaybackStateQueue> playbackItem = _currentPlaybacks.MaxBy(f => f.Value.Priority);
        PlaybackState? playback = playbackItem.Value.PlaybackState;

        string dataFolder = $"{Globals.GetAppDataPath()}data\\";

        if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);

        string statusFile = $"{dataFolder}status.json";
        string artworkPath = $"{dataFolder}cover.jpg";
        Color dominantColor = Color.Black;

        File.Delete(artworkPath);

        // Set empty if not playing
        if (playback is { IsPlaying: false })
        {
            ClearStatusFiles();
            return;
        }

        // Local image empty
        if (playback?.Item is { IsLocal: true })
        {
            SaveEmptyCover();
        }
        // Download cover
        else
        {
            string? coverUrl = playback?.Item?.Album?.Images?.First().Url;

            if (!string.IsNullOrEmpty(coverUrl))
                try
                {
                    byte[] imageBytes = await _httpClient?.GetByteArrayAsync(coverUrl)!;
                    await File.WriteAllBytesAsync(artworkPath, imageBytes);

                    SKImage? image = SKImage.FromEncodedData(artworkPath);
                    SKBitmap? map = SKBitmap.FromImage(image);
                    dominantColor = GetMostUsedColor(map);
                    map.Dispose();
                }
                catch
                {
                    SaveEmptyCover();
                }
            else
                SaveEmptyCover();
        }

        double r = dominantColor.R / 255.0;
        double g = dominantColor.G / 255.0;
        double b = dominantColor.B / 255.0;

        OverlayState state = new()
        {
            Song = new OverlayStateSong
            {
                Artist = JoinArtists(playback?.Item?.Artists),
                Name = playback?.Item?.Name,
                Color = new OverlayStateSongColor
                {
                    Red = r,
                    Green = g,
                    Blue = b
                }
            },
            Status = new OverlayStateStatus
            {
                FetchTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                //FetchTime = playbackState!.Timestamp / 1000,
                Paused = !playback?.IsPlaying ?? false,
                Progress = playback?.ProgressMs ?? 0,
                Total = playback?.Item?.DurationMs ?? 0
            }
        };

        string stateString = JsonConvert.SerializeObject(state);

        await File.WriteAllTextAsync(statusFile, stateString);
    }

    public void ClearStatusFiles()
    {
        string dataFolder = $"{Globals.GetAppDataPath()}data\\";

        if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);

        string statusFile = $"{dataFolder}status.json";
        string artworkPath = $"{dataFolder}cover.jpg";

        File.WriteAllText(statusFile, "{}");
        File.WriteAllBytes(artworkPath, _blankImage);
    }

    public void SaveEmptyCover()
    {
        string dataFolder = $"{Globals.GetAppDataPath()}data\\";

        if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);

        string artworkPath = $"{dataFolder}cover.jpg";

        File.WriteAllBytes(artworkPath, _blankImage);
    }

    private static string JoinArtists(IEnumerable<Artist>? artists)
    {
        if (artists == null) return "";
        string output = artists.Aggregate("", (current, artist) => current + artist.Name + ";");
        return output[..^1];
    }

    private static Color GetMostUsedColor(SKBitmap bitMap)
    {
        ColorThief.ColorThief colorThief = new();
        QuantizedColor color = colorThief.GetColor(bitMap);
        return Color.FromArgb(color.Color.R, color.Color.G, color.Color.B);
    }

    private class PlaybackStateQueue
    {
        public PlaybackState? PlaybackState;
        public int Priority;

        public PlaybackStateQueue(PlaybackState? playbackState, int priority)
        {
            PlaybackState = playbackState;
            Priority = priority;
        }
    }
}