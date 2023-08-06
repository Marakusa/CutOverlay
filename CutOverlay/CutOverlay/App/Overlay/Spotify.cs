using System.Net.Http.Headers;
using CutOverlay.Models;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace CutOverlay.App.Overlay;

[Overlay]
public class Spotify : OAuthOverlayApp
{
    internal static Spotify? Instance;

    private Timer? _statusTimer;

    public Spotify()
    {
        if (Instance != null)
        {
            Dispose();
            return;
        }

        Instance = this;

        HttpClient = new HttpClient();

        AuthorizationTimer = null;
        _statusTimer = null;
    }

    public override string AuthApiUri => "https://accounts.spotify.com/api/token";
    public override string CallbackAddress => $"http://localhost:{Globals.Port}/spotify/callback";
    public override string AuthorizationAddress => "https://accounts.spotify.com/authorize";
    public override string Scopes => "user-read-playback-state user-read-currently-playing user-modify-playback-state";

    public override OverlayApp? GetInstance()
    {
        return Instance;
    }

    public override Task Start(Dictionary<string, string?>? configurations)
    {
        Console.WriteLine("Spotify app starting...");

        if (configurations == null ||
            !configurations.ContainsKey("spotifyClientId") || !configurations.ContainsKey("spotifyClientSecret") ||
            string.IsNullOrEmpty(configurations["spotifyClientId"]) ||
            string.IsNullOrEmpty(configurations["spotifyClientSecret"]))
            return Task.CompletedTask;

        _statusTimer?.Stop();
        _statusTimer = new Timer { Interval = 2000 };
        _statusTimer.Elapsed += async (_, _) => { await FetchStatusAsync(); };
        _statusTimer.Start();

        SetupOAuth(configurations["spotifyClientId"], configurations["spotifyClientSecret"]);

        Console.WriteLine("Spotify app started!");
        return Task.CompletedTask;
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

            await Status.Instance?.SaveStateAsync<Spotify>(playbackState,
                playbackState is not { IsPlaying: true } ? -1 : 5)!;
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
            Status.Instance?.ClearStatusFiles();
        }
        catch
        {
            // ignored
        }

        AuthorizationTimer?.Dispose();
        _statusTimer?.Dispose();
        HttpClient?.Dispose();

        Console.WriteLine("Spotify app unloaded");
    }
}