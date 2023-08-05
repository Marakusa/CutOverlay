using System.Net.WebSockets;
using System.Text;
using CutOverlay.Models;
using CutOverlay.Models.BeatSaberPlus;
using CutOverlay.Models.BeatSaberPlus.App;
using CutOverlay.Models.BeatSaberPlus.BeatSaver;
using Newtonsoft.Json;
using Image = CutOverlay.Models.Image;

namespace CutOverlay.App;

[Overlay]
public class BeatSaberPlus : OverlayApp
{
    internal static BeatSaberPlus? Instance;

    private const int BufferSize = 4096;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _reconnectInterval = 1000;
    private const string WebsocketAddress = "ws://localhost:2947/socket";
    private const string MapDataUrl = "https://api.beatsaver.com/maps/hash/{0}";
    
    public BeatSaberPlus()
    {
        if (Instance != null)
        {
            Dispose();
            return;
        }

        Instance = this;

        HttpClient = new HttpClient();
    }

    public override async Task Start(Dictionary<string, string?>? configurations)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        while (_cancellationTokenSource is { Token.IsCancellationRequested: false })
        {
            try
            {
                await SetupWebSocket();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
                await Task.Delay(3000); // Wait for 3 seconds before attempting reconnection
            }
        }
    }

    private async Task SetupWebSocket()
    {
        Console.WriteLine("BSPlus web socket setting up...");

        using ClientWebSocket webSocket = new();
        try
        {
            await webSocket.ConnectAsync(new Uri(WebsocketAddress), CancellationToken.None);

            if (webSocket.State == WebSocketState.Open)
                Console.WriteLine("BeatSaberPlus app started!");
            else
                throw new Exception("Failed to connect to web socket");

            while (_cancellationTokenSource is { Token.IsCancellationRequested: false }) await ReceiveMessage(webSocket, _cancellationTokenSource);
        }
        catch (Exception ex)
        {
            Console.WriteLine("WebSocket error: " + ex.Message);
            // Attempt reconnection after a certain interval
            await Task.Delay(_reconnectInterval);
            // Increase the reconnect interval for the next attempt
            _reconnectInterval = Math.Min(_reconnectInterval * 2, 60000); // Max 1 minute
            _ = SetupWebSocket();
        }
    }

    private async Task ReceiveMessage(WebSocket webSocket, CancellationTokenSource cancellationTokenSource)
    {
        var buffer = new ArraySegment<byte>(new byte[BufferSize]);
        StringBuilder receivedData = new();
        WebSocketReceiveResult result;

        do
        {
            result = await webSocket.ReceiveAsync(buffer, cancellationTokenSource.Token);
            if (buffer.Array == null) continue;
            string data = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
            receivedData.Append(data);
        }
        while (!result.EndOfMessage);

        try
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = receivedData.ToString();

                BeatSaberPlusWebhookBase? webHook = JsonConvert.DeserializeObject<BeatSaberPlusWebhookBase>(message);
                switch (webHook?.Type)
                {
                    case "handshake":
                        break;
                    case "event":
                        BeatSaberPlusWebHookEvent? eventData = JsonConvert.DeserializeObject<BeatSaberPlusWebHookEvent>(message);
                        switch (eventData?.Event)
                        {
                            case "gameState":
                                BeatSaberPlusWebHookGameState? gameState = JsonConvert.DeserializeObject<BeatSaberPlusWebHookGameState>(message);
                                if (gameState?.GameStateChanged == "Menu")
                                {
                                    await StatusApp.Instance?.SaveStateAsync<BeatSaberPlus>(new PlaybackState(), -1)!;
                                    StatusApp.Instance.SaveEmptyCover();
                                }
                                break;
                            case "mapInfo":
                                BeatSaberPlusWebHookMapInfoChanged? mapInfo = JsonConvert.DeserializeObject<BeatSaberPlusWebHookMapInfoChanged>(message);
                                PlaybackState playbackState = new()
                                {
                                    Timestamp = 0,
                                    ProgressMs = 0,
                                    IsPlaying = true,
                                    Item = new Item
                                    {
                                        Album = new Album
                                        {
                                            Images = new List<Image>
                                            {
                                                new()
                                                {
                                                    Url = null
                                                }
                                            }
                                        },
                                        Artists = new List<Artist>
                                        {
                                            new()
                                            {
                                                Name = mapInfo?.MapInfoChanged.Artist
                                            }
                                        },
                                        Name = mapInfo?.MapInfoChanged.Name,
                                        IsLocal = false,
                                        DurationMs = mapInfo!.MapInfoChanged.Duration
                                    }
                                };

                                string id = mapInfo.MapInfoChanged.LevelId;
                                if (id.StartsWith("custom_level_"))
                                    id = id["custom_level_".Length..];

                                try
                                {
                                    HttpResponseMessage response = await HttpClient?.GetAsync(string.Format(MapDataUrl, id))!;

                                    if (!response.IsSuccessStatusCode)
                                    {
                                        Console.WriteLine($"No map data found. Status code: {response.StatusCode}");
                                        throw new Exception();
                                    }

                                    string json = await response.Content.ReadAsStringAsync();
                                    BeatSaverMapData? mapData = JsonConvert.DeserializeObject<BeatSaverMapData>(json);
                                    
                                    if (mapData is { Versions.Count: > 0 })
                                    {
                                        string? cover = mapData.Versions[0].CoverUrl;
                                        playbackState.Item.Album.Images[0].Url = cover;
                                    }
                                    else
                                    {
                                        Console.WriteLine("No map data available.");
                                    }
                                }
                                catch (Exception)
                                {
                                    // ignore
                                }

                                await StatusApp.Instance?.SaveStateAsync<BeatSaberPlus>(playbackState, 10)!;
                                break;
                            case "score":
                                /*BeatSaberPlusWebHookScoreEvent? scoreEvent = JsonConvert.DeserializeObject<BeatSaberPlusWebHookScoreEvent>(message);
                                _currentScore = new BeatSaberAppScoreData
                                {
                                    Time = scoreEvent.ScoreEvent.Time,
                                    Score = scoreEvent.ScoreEvent.Score,
                                    Accuracy = scoreEvent.ScoreEvent.Accuracy,
                                    Combo = scoreEvent.ScoreEvent.Combo,
                                    MissCount = scoreEvent.ScoreEvent.MissCount,
                                    CurrentHealth = scoreEvent.ScoreEvent.CurrentHealth
                                };
                                await File.WriteAllTextAsync(@Application.StartupPath + @"\Snip_BSPlusScore.txt", JsonConvert.SerializeObject(_currentScore));*/
                                break;
                        }
                        break;
                }
            }
        }
        catch (Exception)
        {
            // ignore
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
        
        HttpClient?.Dispose();
    }
}