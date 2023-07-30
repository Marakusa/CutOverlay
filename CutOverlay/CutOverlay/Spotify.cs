using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using CutOverlay.Models;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace CutOverlay;

public class Spotify
{
    internal static Spotify? Instance;

    private const string AuthorizationAddress = "https://accounts.spotify.com/authorize";
    private const string Scopes = "user-read-playback-state user-read-currently-playing user-modify-playback-state";
    private const string ResponseType = "code";
    private readonly string _callbackAddress = $"http://localhost:{Globals.Port}/spotify/callback";
    private readonly HttpClient? _httpClient;
    public string? AccessToken;
    private string? _refreshToken;

    public Spotify()
    {
        _httpClient = new HttpClient();
        Instance = this;
    }

    public async Task Start()
    {
        Dictionary<string, string?>? configurations = await FetchConfigurationsAsync();

        Timer authorizationTimer = new() { Interval = 200000 };
        authorizationTimer.Elapsed += async (_, _) => { await UpdateAuthorizationAsync(); };
        authorizationTimer.Start();

        Timer statusTimer = new() { Interval = 2000 };
        statusTimer.Elapsed += async (_, _) => { await FetchStatusAsync(); };
        statusTimer.Start();

        Process.Start(new ProcessStartInfo
        {
            FileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?client_id={1}&response_type={2}&redirect_uri={3}&scope={4}",
                AuthorizationAddress,
                configurations?["spotifyClientId"],
                ResponseType,
                _callbackAddress,
                Scopes
            ),
            UseShellExecute = true
        });
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

            HttpResponseMessage response = await _httpClient!.SendAsync(request);
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

            HttpResponseMessage response = await _httpClient!.SendAsync(request);
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

    private static async Task SaveStateAsync(SpotifyPlaybackState? playbackState)
    {
        OverlayState state = new()
        {
            Song = new OverlayStateSong
            {
                Artist = JoinArtists(playbackState?.Item?.Artists),
                Name = playbackState?.Item?.Name,
                Color = new OverlayStateSongColor
                {
                    Red = 0.8,
                    Green = 0.2,
                    Blue = 0.6
                }
            },
            Status = new OverlayStateStatus
            {
                FetchTime = playbackState!.Timestamp,
                Paused = playbackState.IsPlaying,
                Progress = playbackState.ProgressMs,
                Total = playbackState.DurationMs
            }
        };

        string dataFolder = $"{AppContext.BaseDirectory}data\\";
        string statusFile = $"{dataFolder}status.json";

        string stateString = JsonConvert.SerializeObject(state);

        if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
        await File.WriteAllTextAsync(statusFile, stateString);
    }

    private static string JoinArtists(IEnumerable<Artist>? artists)
    {
        if (artists == null) return "";
        string output = artists.Aggregate("", (current, artist) => current + artist.Name + ";");
        return output[..^1];
    }

    private async Task<Dictionary<string, string?>?> FetchConfigurationsAsync()
    {
        HttpResponseMessage configurationResponse = await _httpClient!.GetAsync($"http://localhost:{Globals.Port}/configuration");
        string configurationJson = await configurationResponse.Content.ReadAsStringAsync();
        if (configurationResponse.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<Dictionary<string, string?>>(configurationJson);
        Console.WriteLine($"ERROR: {configurationJson}");
        return new Dictionary<string, string?>();
    }
}