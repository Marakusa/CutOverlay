using System.Timers;
using Timer = System.Timers.Timer;

namespace CutOverlay;

public class Spotify
{
    private readonly HttpClient _httpClient;

    public Spotify(IHttpClientFactory httpClient)
    {
        _httpClient = httpClient.CreateClient();
    }

    public async Task Start()
    {
        Timer timer = new()
        {
            Interval = 1000
        };
        timer.Elapsed += async (sender, args) =>
        {
            await FetchStatusAsync(sender, args);
        };
        timer.Start();
    }

    private async Task FetchStatusAsync(object? sender, ElapsedEventArgs e)
    {
        const string apiUrl = "https://accounts.spotify.com/api/token";

        FormUrlEncodedContent form = new(new[]
        {
            new KeyValuePair<string, string?>("grant_type", "client_credentials"),
            new KeyValuePair<string, string?>("client_id", ConfigurationController.Configurations?["spotifyClientId"]),
            new KeyValuePair<string, string?>("client_secret", ConfigurationController.Configurations?["spotifyClientSecret"])
        });
        HttpRequestMessage request = new(HttpMethod.Post, apiUrl)
        {
            Content = form
        };
        HttpResponseMessage response = await _httpClient.SendAsync(request);
        string s = await response.Content.ReadAsStringAsync();
        Console.WriteLine(s);
    }
}