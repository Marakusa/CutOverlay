﻿using CutOverlay.App;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using TwitchLib.Api.Helix.Models.Entitlements;

namespace CutOverlay.Services;

public class LoggerService : OverlayApp
{
    public event EventHandler<LogEventArgs>? OnLog;

    private static readonly List<WebSocket> Sockets = new();

    public LogLevel LogLevel { get; set; }

    public LoggerService()
    {
        Status = ServiceStatusType.Starting;

        LogLevel = LogLevel.Information;

        _ = SetupWebSocketAsync();
    }

    private async Task SetupWebSocketAsync()
    {
        HttpListener listener = new();
        listener.Prefixes.Add(
            $"http://localhost:{Globals.LoggerWebSocketPort}/"); // Replace with your desired address

        listener.Start();
        Status = ServiceStatusType.Running;

        LogInformation("Logger WebSocket started");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
                ProcessWebSocketRequest(context);
            else
                context.Response.Close();
        }
    }
    
    private async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        HttpListenerWebSocketContext socketContext = await context.AcceptWebSocketAsync(null);
        WebSocket socket = socketContext.WebSocket;

        LogInformation("New connection to logger WebSocket");
        
        for (int i = 0; i < Sockets.Count; i++)
        {
            if (Sockets[i].State == WebSocketState.Open) continue;
            Sockets[i] = socket;
            return;
        }
        Sockets.Add(socket);
    }

    public async Task SendMessageToAllAsync(string message)
    {
        foreach (WebSocket socket in Sockets)
        {
            if (socket.State != WebSocketState.Open)
            {
                socket.Abort();
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                socket.Dispose();
                continue;
            }
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
    }

    public override void Unload() { }

    public override ServiceStatusType GetStatus() => Status;

    public void LogTrace(object? message)
    {
        Log(LogLevel.Trace, message);
    }

    public void LogDebug(object? message)
    {
        Log(LogLevel.Debug, message);
    }

    public void LogInformation(object? message)
    {
        Log(LogLevel.Information, message);
    }

    public void LogWarning(object? message)
    {
        Log(LogLevel.Warning, message);
    }

    public void LogError(object? message)
    {
        Log(LogLevel.Error, message);
    }

    public void LogCritical(object? message)
    {
        Log(LogLevel.Critical, message);
    }

    public void Log(LogLevel logLevel, object? message)
    {
        try
        {
            if (message == null || (int)LogLevel > (int)logLevel)
                return;

            string log = $"[{logLevel}]: {message}";
            Console.WriteLine(log);

            OnLog?.Invoke(this, new LogEventArgs(logLevel, log));
            _ = SendMessageToAllAsync(log);
        }
        catch
        {
            // ignored
        }
    }
}

public class LogEventArgs
{
    public LogLevel LogLevel { get; private set; }
    public string? Message { get; private set; }

    public LogEventArgs(LogLevel logLevel, string? message)
    {
        LogLevel = logLevel;
        Message = message;
    }
}