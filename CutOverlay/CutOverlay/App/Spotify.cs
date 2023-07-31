using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using ColorThief;
using CutOverlay.Models;
using Newtonsoft.Json;
using SkiaSharp;
using Color = System.Drawing.Color;
using Timer = System.Timers.Timer;

namespace CutOverlay.App;

public class Spotify : OverlayApp
{
    private const string AuthorizationAddress = "https://accounts.spotify.com/authorize";
    private const string Scopes = "user-read-playback-state user-read-currently-playing user-modify-playback-state";
    private const string ResponseType = "code";
    internal static Spotify? Instance;

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

    private readonly string _callbackAddress = $"http://localhost:{Globals.Port}/spotify/callback";
    private Timer? _authorizationTimer;
    private string? _refreshToken;
    private Timer? _statusTimer;
    public string? AccessToken;

    public Spotify()
    {
        if (Instance != null)
        {
            Dispose();
            return;
        }

        Instance = this;

        _authorizationTimer = null;
        _statusTimer = null;
    }

    public override async Task Start(Dictionary<string, string?>? configurations)
    {
        Console.WriteLine("Spotify app starting...");

        string dataFolder = $"{Globals.GetAppDataPath()}data\\";

        if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);

        string statusFile = $"{dataFolder}status.json";
        string artworkPath = $"{dataFolder}cover.jpg";

        await File.WriteAllTextAsync(statusFile, "{}");
        SaveEmptyCover(artworkPath);

        if (configurations == null ||
            !configurations.ContainsKey("spotifyClientId") || !configurations.ContainsKey("spotifyClientSecret") ||
            string.IsNullOrEmpty(configurations["spotifyClientId"]) ||
            string.IsNullOrEmpty(configurations["spotifyClientSecret"]))
            return;

        _authorizationTimer?.Stop();
        _authorizationTimer = new Timer { Interval = 200000 };
        _authorizationTimer.Elapsed += async (_, _) => { await UpdateAuthorizationAsync(); };
        _authorizationTimer.Start();

        _statusTimer?.Stop();
        _statusTimer = new Timer { Interval = 2000 };
        _statusTimer.Elapsed += async (_, _) => { await FetchStatusAsync(); };
        _statusTimer.Start();

        Process.Start(new ProcessStartInfo
        {
            FileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?client_id={1}&response_type={2}&redirect_uri={3}&scope={4}",
                AuthorizationAddress,
                configurations["spotifyClientId"],
                ResponseType,
                _callbackAddress,
                Scopes
            ),
            UseShellExecute = true
        });

        Console.WriteLine("Spotify app started!");
    }

    public async Task UpdateAuthorizationAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(AccessToken))
                return;

            const string apiUrl = "https://accounts.spotify.com/api/token";

            Dictionary<string, string?>? configurations = await FetchConfigurationsAsync();
            HttpRequestMessage request;

            if (string.IsNullOrEmpty(_refreshToken))
            {
                request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string?>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string?>("code", AccessToken),
                        new KeyValuePair<string, string?>("redirect_uri", _callbackAddress)
                    })
                };
                request.Headers.Add("User-Agent", "CutOverlay/" + Assembly.GetExecutingAssembly().GetName().Version);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            $"{configurations?["spotifyClientId"]}:{configurations?["spotifyClientSecret"]}")));
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string?>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string?>("refresh_token", _refreshToken)
                    })
                };
                request.Headers.Add("User-Agent", "CutOverlay/" + Assembly.GetExecutingAssembly().GetName().Version);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            $"{configurations?["spotifyClientId"]}:{configurations?["spotifyClientSecret"]}")));
            }

            HttpResponseMessage response = await HttpClient!.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ERROR: {content}");
                return;
            }

            SpotifyAuthenticationModel? authentication =
                JsonConvert.DeserializeObject<SpotifyAuthenticationModel>(content);
            AccessToken = authentication?.AccessToken;
            if (!string.IsNullOrEmpty(authentication?.RefreshToken))
                _refreshToken = authentication?.RefreshToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
        }
    }

    private async Task FetchStatusAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(AccessToken))
                return;

            const string playbackStateUrl = "https://api.spotify.com/v1/me/player/currently-playing";

            HttpRequestMessage request = new(HttpMethod.Get, playbackStateUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            HttpResponseMessage response = await HttpClient!.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ERROR: {content}");
                return;
            }

            SpotifyPlaybackState? playbackState = JsonConvert.DeserializeObject<SpotifyPlaybackState>(content);

            await SaveStateAsync(playbackState);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
        }
    }

    private async Task SaveStateAsync(SpotifyPlaybackState? playbackState)
    {
        string dataFolder = $"{Globals.GetAppDataPath()}data\\";

        if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);

        string statusFile = $"{dataFolder}status.json";
        string artworkPath = $"{dataFolder}cover.jpg";
        Color dominantColor = Color.Black;

        File.Delete(artworkPath);

        // Set empty if not playing
        if (playbackState is { IsPlaying: false })
        {
            await File.WriteAllTextAsync(statusFile, "{}");
            SaveEmptyCover(artworkPath);
            return;
        }

        // Local image empty
        if (playbackState?.Item is { IsLocal: true })
        {
            SaveEmptyCover(artworkPath);
        }
        // Download cover
        else
        {
            string? coverUrl = playbackState?.Item?.Album?.Images?.First().Url;

            if (!string.IsNullOrEmpty(coverUrl))
                try
                {
                    byte[] imageBytes = await HttpClient?.GetByteArrayAsync(coverUrl)!;
                    await File.WriteAllBytesAsync(artworkPath, imageBytes);

                    SKImage? image = SKImage.FromEncodedData(artworkPath);
                    SKBitmap? map = SKBitmap.FromImage(image);
                    dominantColor = GetMostUsedColor(map);
                    map.Dispose();
                }
                catch
                {
                    SaveEmptyCover(artworkPath);
                }
            else
                SaveEmptyCover(artworkPath);
        }

        double r = dominantColor.R / 255.0;
        double g = dominantColor.G / 255.0;
        double b = dominantColor.B / 255.0;

        OverlayState state = new()
        {
            Song = new OverlayStateSong
            {
                Artist = JoinArtists(playbackState?.Item?.Artists),
                Name = playbackState?.Item?.Name,
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
                Paused = !playbackState?.IsPlaying ?? false,
                Progress = playbackState?.ProgressMs ?? 0,
                Total = playbackState?.Item?.DurationMs ?? 0
            }
        };

        string stateString = JsonConvert.SerializeObject(state);

        await File.WriteAllTextAsync(statusFile, stateString);
    }

    private void SaveEmptyCover(string artworkPath)
    {
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

    public override void Unload()
    {
        try
        {
            string dataFolder = $"{Globals.GetAppDataPath()}data\\";

            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);

            string statusFile = $"{dataFolder}status.json";
            string artworkPath = $"{dataFolder}cover.jpg";

            File.WriteAllText(statusFile, "{}");
            SaveEmptyCover(artworkPath);
        }
        catch
        {
            // ignored
        }

        _authorizationTimer?.Dispose();
        _statusTimer?.Dispose();
        HttpClient?.Dispose();
    }
}