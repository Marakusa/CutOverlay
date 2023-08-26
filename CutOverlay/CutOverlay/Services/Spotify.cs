using System.Net.Http.Headers;
using CutOverlay.App;
using CutOverlay.Models;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace CutOverlay.Services;

public class Spotify : OAuthOverlayApp
{
    private Timer? _statusTimer;
    private readonly OverlayStatusService _overlayStatus;
    private readonly ConfigurationService _configurationService;

    public Spotify(OverlayStatusService overlayStatus, ConfigurationService configurationService)
    {
        HttpClient = new HttpClient();
        _configurationService = configurationService;
        _overlayStatus = overlayStatus;

        AuthorizationTimer = null;
        _statusTimer = null;

        _ = Task.Run(async () =>
        {
            await Start(await _configurationService.FetchConfigurationsAsync());
        });
    }

    public override string AuthApiUri => "https://accounts.spotify.com/api/token";
    public override string CallbackAddress => $"http://localhost:{Globals.Port}/spotify/callback";
    public override string AuthorizationAddress => "https://accounts.spotify.com/authorize";
    public override string Scopes => "user-read-playback-state user-read-currently-playing user-modify-playback-state";
    
    public async Task RefreshConfigurationsAsync()
    {
        AuthorizationTimer?.Stop();
        _statusTimer?.Stop();
        
        await Start(await _configurationService.FetchConfigurationsAsync());
    }

    public Task Start(Dictionary<string, string?>? configurations)
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

            await _overlayStatus.SaveStateAsync<Spotify>(playbackState,
                playbackState is not { IsPlaying: true } ? -1 : 5);
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
            _overlayStatus.ClearStatusFiles();
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