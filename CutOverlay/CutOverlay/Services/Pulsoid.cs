﻿using System.Net.WebSockets;
using System.Text;
using CutOverlay.App;
using CutOverlay.Models;
using Newtonsoft.Json;

namespace CutOverlay.Services;

public class Pulsoid : OverlayApp
{
    private readonly ConfigurationService _configurationService;
    private string? _heartBeat;
    private string? _pulsoidApiToken;
    private int _reconnectInterval = 1000;
    private ClientWebSocket? _socket;

    public Pulsoid(ConfigurationService configurationService)
    {
        HttpClient = new HttpClient();
        _configurationService = configurationService;

        _socket = null;

        _ = Task.Run(async () => { await Start(await _configurationService.FetchConfigurationsAsync()); });
    }

    public async Task RefreshConfigurationsAsync()
    {
        await _socket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Restarting the service",
            CancellationToken.None)!;
        _socket.Dispose();
        _socket = null;
        await Start(await _configurationService.FetchConfigurationsAsync());
    }

    public virtual Task Start(Dictionary<string, string?>? configurations)
    {
        Console.WriteLine("Pulsoid app starting...");

        if (configurations == null ||
            !configurations.ContainsKey("pulsoidAccessToken") ||
            string.IsNullOrEmpty(configurations["pulsoidAccessToken"]))
        {
            Console.WriteLine("Pulsoid access token missing");
            return Task.CompletedTask;
        }

        _pulsoidApiToken = configurations["pulsoidAccessToken"];
        _ = SetupWebSocket();

        Console.WriteLine("Pulsoid app started!");
        return Task.CompletedTask;
    }

    private async Task SetupWebSocket()
    {
        Console.WriteLine("Pulsoid web socket setting up...");

        string url = $"wss://dev.pulsoid.net/api/v1/data/real_time?access_token={_pulsoidApiToken}";

        using ClientWebSocket webSocket = new();
        try
        {
            await webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
            _socket = webSocket;

            while (_socket.State == WebSocketState.Open) await ReceiveMessage(_socket);
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

    private async Task ReceiveMessage(WebSocket? webSocket)
    {
        byte[] buffer = new byte[1024];
        WebSocketReceiveResult result =
            await webSocket?.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)!;
        if (result.MessageType == WebSocketMessageType.Text)
        {
            string data = Encoding.UTF8.GetString(buffer, 0, result.Count);
            // Parse JSON data and call the updateHeartRate method with the data
            UpdateHeartRate(data);
        }
    }

    private void UpdateHeartRate(string data)
    {
        PulsoidResponse? response = JsonConvert.DeserializeObject<PulsoidResponse>(data);
        _heartBeat = response == null ? "" : response.Data.HeartRate.ToString();
    }

    public override void Unload()
    {
        _socket?.Dispose();
        HttpClient?.Dispose();

        Console.WriteLine("Pulsoid app unloaded");
    }

    public string GetHeartBeat()
    {
        return _heartBeat ?? "";
    }
}