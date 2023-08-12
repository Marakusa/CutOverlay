using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using CutOverlay.Models.Spotify;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace CutOverlay.App.Overlay;

public abstract class OAuthOverlayApp : OverlayApp
{
    private const string ResponseType = "code";

    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly Random Random = new();

    private string? _clientId = "";
    private string? _clientSecret = "";
    private string _stateToken = "";
    private protected string? AccessToken;

    private protected Timer? AuthorizationTimer;
    private protected string? RefreshToken;
    public abstract string AuthApiUri { get; }
    public abstract string CallbackAddress { get; }
    public abstract string AuthorizationAddress { get; }
    public abstract string Scopes { get; }

    private string RegenerateStateToken()
    {
        _stateToken = new string(Enumerable.Repeat(Chars, 32).Select(s => s[Random.Next(s.Length)]).ToArray());
        return _stateToken;
    }

    private protected void SetupOAuth(string? clientId, string? clientSecret)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;

        AuthorizationTimer?.Stop();
        AuthorizationTimer = new Timer { Interval = 120000 };
        AuthorizationTimer.Elapsed += async (_, _) => { await UpdateAuthorizationAsync(); };
        AuthorizationTimer.Start();

        string state = RegenerateStateToken();

        Process.Start(new ProcessStartInfo
        {
            FileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?response_type={1}&client_id={2}&redirect_uri={3}&scope={4}&state={5}",
                AuthorizationAddress,
                ResponseType,
                _clientId,
                CallbackAddress,
                Scopes,
                state
            ),
            UseShellExecute = true
        });
    }

    private async Task UpdateAuthorizationAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(AccessToken))
                return;

            HttpRequestMessage request;

            string state = RegenerateStateToken();

            if (string.IsNullOrEmpty(RefreshToken))
            {
                request = new HttpRequestMessage(HttpMethod.Post, AuthApiUri)
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string?>("client_id", _clientId),
                        new KeyValuePair<string, string?>("client_secret", _clientSecret),
                        new KeyValuePair<string, string?>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string?>("code", AccessToken),
                        new KeyValuePair<string, string?>("state", state),
                        new KeyValuePair<string, string?>("redirect_uri", CallbackAddress)
                    })
                };
                request.Headers.Add("User-Agent", "CutOverlay/" + Assembly.GetExecutingAssembly().GetName().Version);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            $"{_clientId}:{_clientSecret}")));
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Post, AuthApiUri)
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string?>("client_id", _clientId),
                        new KeyValuePair<string, string?>("client_secret", _clientSecret),
                        new KeyValuePair<string, string?>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string?>("state", state),
                        new KeyValuePair<string, string?>("refresh_token", RefreshToken)
                    })
                };
                request.Headers.Add("User-Agent", "CutOverlay/" + Assembly.GetExecutingAssembly().GetName().Version);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            $"{_clientId}:{_clientSecret}")));
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
                RefreshToken = authentication?.RefreshToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
        }
    }

    public void AuthCallback(string code, string state)
    {
        if (string.IsNullOrEmpty(state) || !state.Equals(state, StringComparison.InvariantCulture))
            throw new Exception("Failed to verify the request");

        AccessToken = code;
        _ = UpdateAuthorizationAsync();
    }

    public abstract override OverlayApp? GetInstance();

    public abstract override Task Start(Dictionary<string, string?>? configurations);

    public abstract override void Unload();
}