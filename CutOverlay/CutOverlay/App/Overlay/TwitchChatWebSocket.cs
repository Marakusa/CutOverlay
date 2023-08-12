using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;

namespace CutOverlay.App.Overlay;

public class TwitchChatWebSocket
{
    private static readonly HttpClient HttpClient = new();
    private static readonly List<WebSocket> Sockets = new();
    private static string? _username;
    private static string? _userId;

    public void Start(string? username, string userId)
    {
        _username = username;
        _userId = userId;

        Console.WriteLine($"Starting WebSocket for {username} ({userId}) chat");

        _ = StartChatWebSocket();
    }

    public void Stop()
    {
        foreach (WebSocket socket in Sockets) socket.Dispose();
    }

    private static async Task StartChatWebSocket()
    {
        HttpListener listener = new();
        listener.Prefixes.Add(
            $"http://localhost:{Globals.ChatWebSocketPort}/"); // Replace with your desired address

        listener.Start();
        Console.WriteLine("Listening for WebSocket connections...");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
                ProcessWebSocketRequest(context);
            else
                context.Response.Close();
        }
    }
    
    private static async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        HttpListenerWebSocketContext socketContext = await context.AcceptWebSocketAsync(null);
        WebSocket socket = socketContext.WebSocket;

        Console.WriteLine("WebSocket connection established.");
        Sockets.Add(socket);
    }

    public async Task SendMessageToAllAsync(string message)
    {
        foreach (WebSocket socket in Sockets)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

public class UserInfoResponse
{
    [JsonProperty("id")] public string? Id { get; set; }

    [JsonProperty("bots")] public List<string>? Bots { get; set; }

    [JsonProperty("channelEmotes")] public List<Emote>? ChannelEmotes { get; set; }

    [JsonProperty("sharedEmotes")] public List<Emote>? SharedEmotes { get; set; }
}

public class Emote
{
    [JsonProperty("id")] public string? Id { get; set; }

    [JsonProperty("code")] public string? Code { get; set; }

    [JsonProperty("imageType")] public string? ImageType { get; set; }

    [JsonProperty("animated")] public bool Animated { get; set; }

    [JsonProperty("user")] public UserInfo? User { get; set; }
}

public class UserInfo
{
    [JsonProperty("id")] public string? Id { get; set; }

    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("displayName")] public string? DisplayName { get; set; }

    [JsonProperty("providerId")] public string? ProviderId { get; set; }
}