using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using CutOverlay.Models;
using CutOverlay.Models.Spotify;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace CutOverlay.App;

[Overlay]
public class Spotify : OverlayApp
{
    private const string AuthorizationAddress = "https://accounts.spotify.com/authorize";
    private const string Scopes = "user-read-playback-state user-read-currently-playing user-modify-playback-state";
    private const string ResponseType = "code";
    internal static Spotify? Instance;

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

        HttpClient = new HttpClient();

        _authorizationTimer = null;
        _statusTimer = null;
    }
    
    public override Task Start(Dictionary<string, string?>? configurations)
    {
        Console.WriteLine("Spotify app starting...");
        
        if (configurations == null ||
            !configurations.ContainsKey("spotifyClientId") || !configurations.ContainsKey("spotifyClientSecret") ||
            string.IsNullOrEmpty(configurations["spotifyClientId"]) ||
            string.IsNullOrEmpty(configurations["spotifyClientSecret"]))
            return Task.CompletedTask;

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
        return Task.CompletedTask;
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

            PlaybackState? playbackState = JsonConvert.DeserializeObject<PlaybackState>(content);

            await StatusApp.Instance?.SaveStateAsync<Spotify>(playbackState, playbackState is not { IsPlaying: true } ? -1 : 5)!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
        }
    }

    public override void Unload()
    {
        try
        {
            StatusApp.Instance?.ClearStatusFiles();
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