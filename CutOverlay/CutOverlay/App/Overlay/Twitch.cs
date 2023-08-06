using ElectronNET.API;
using System.Diagnostics;
using System.Globalization;
using CutOverlay.Models.Twitch;
using Newtonsoft.Json;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using Timer = System.Timers.Timer;

namespace CutOverlay.App.Overlay;

[Overlay]
public class Twitch : OverlayApp
{
    private const string ClientId = "nrlnctz147p5v5xwy2rhjlas2afh3s";
    private string _stateToken = "";

    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly Random Random = new(DateTime.Now.Second + DateTime.Now.DayOfYear + DateTime.Now.Year + DateTime.Now.Millisecond);

    internal static Twitch? Instance;
    
    public Twitch()
    {
        if (Instance != null)
        {
            Dispose();
            return;
        }

        Instance = this;

        HttpClient = new HttpClient();

        _accessToken = null;
        _twitchBotClient = null;
        _twitchApi = null;
        _followerService = null;
    }
    
    private readonly string _callbackAddress = $"http://localhost:{Globals.Port}/twitch/callback";
    private const string AuthorizationAddress = "https://id.twitch.tv/oauth2/authorize";
    private const string ResponseType = "token";
    private const string Scopes = "chat%3Aread+whispers%3Aread+moderation%3Aread+moderator%3Aread%3Afollowers+user%3Aread%3Aemail";

    private TwitchClient? _twitchBotClient;
    private Dictionary<string, string?>? _configurations;

    private static TwitchAPI? _twitchApi;
    private static FollowerService? _followerService;
    private string? _accessToken;

    private readonly List<ChannelFollower> _followers = new();
    private DateTime _lastFetch = DateTime.UnixEpoch;

    public override OverlayApp? GetInstance()
    {
        return Instance;
    }

    public override Task Start(Dictionary<string, string?>? configurations)
    {
        Console.WriteLine("Twitch app starting...");

        _configurations = configurations;

        if (string.IsNullOrEmpty(_configurations?["twitchUsername"]))
            return Task.CompletedTask;
        
        SetupOAuth(ClientId);

        Console.WriteLine("Twitch app started!");
        return Task.CompletedTask;
    }

    private string RegenerateStateToken()
    {
        _stateToken = new string(Enumerable.Repeat(Chars, 32).Select(s => s[Random.Next(s.Length)]).ToArray());
        return _stateToken;
    }

    private void SetupOAuth(string clientId)
    {
        string state = RegenerateStateToken();

        Process.Start(new ProcessStartInfo
        {
            FileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?response_type={1}&client_id={2}&redirect_uri={3}&scope={4}&state={5}",
                AuthorizationAddress,
                ResponseType,
                clientId,
                _callbackAddress,
                Scopes,
                state
            ),
            UseShellExecute = true
        });
    }

    public async Task SetOAuth(string? oAuth, string state)
    {
        if (string.IsNullOrEmpty(state) || !state.Equals(state, StringComparison.InvariantCulture))
            throw new Exception("Failed to verify the request");

        _accessToken = oAuth;

        ConnectionCredentials credentials = new(_configurations?["twitchUsername"], $"oauth:{oAuth}");

        _twitchBotClient?.Disconnect();

        _twitchBotClient = new TwitchClient();
        _twitchBotClient.Initialize(credentials, _configurations?["twitchUsername"]);

        _twitchBotClient.OnError += OnError;
        _twitchBotClient.OnJoinedChannel += OnJoinedChannel;
        _twitchBotClient.OnNewSubscriber += OnNewSubscriber;
        _twitchBotClient.OnRaidNotification += OnRaidNotification;

        _twitchBotClient.Connect();

        _twitchApi = new TwitchAPI
        {
            Settings =
            {
                ClientId = ClientId,
                AccessToken = _accessToken,
                Scopes = new List<AuthScopes>
                {
                    AuthScopes.Helix_Moderation_Read,
                    AuthScopes.Helix_Moderator_Read_Followers,
                    AuthScopes.Helix_User_Read_Follows
                }
            }
        };

        _followerService = new FollowerService(_twitchApi, 5, invokeEventsOnStartup: true);
        _followerService.OnNewFollowersDetected += OnNewFollowers;
        _followerService.OnServiceStarted += OnFollowerServiceStarted;

        GetUsersResponse? users = await _twitchApi.Helix.Users.GetUsersAsync();
        _followerService.SetChannelsById(new List<string>
        {
            users.Users[0].Id
        });

        _followerService.ClearCache();
        _followerService.Start();
    }

    public override void Unload()
    {
        _twitchBotClient?.Disconnect();
        _followerService?.Stop();

        HttpClient?.Dispose();

        _accessToken = null;
        _twitchBotClient = null;
        _twitchApi = null;
        _followerService = null;

        Console.WriteLine("Twitch app unloaded");
    }

    private void OnError(object? sender, OnErrorEventArgs e)
    {
        Console.WriteLine($"Failed to connect: {e.Exception.Message}");
    }

    private void OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine($"Bot {e.BotUsername} has joined the channel {e.Channel}");
    }

    private void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {
        Console.WriteLine($"New subscriber alert! User: {e.Subscriber.DisplayName}, Sub Plan: {e.Subscriber.SubscriptionPlan}");
    }

    private void OnRaidNotification(object? sender, OnRaidNotificationArgs e)
    {
        Console.WriteLine($"Raid alert! User: {e.RaidNotification.DisplayName}, Raid Count: {e.RaidNotification.MsgParamViewerCount}");
    }

    private void OnFollowerServiceStarted(object? sender, OnServiceStartedArgs e)
    {
        Console.WriteLine("Follower service started");
    }

    private void OnNewFollowers(object? sender, OnNewFollowersDetectedArgs e)
    {
        foreach (ChannelFollower follower in e.NewFollowers)
        {
            _followers.Add(follower);
        }

        _lastFetch = DateTime.UtcNow;
    }

    public NewFollowerData GetFollowers(DateTime since)
    {
        List<string> list = (from follower in _followers
            let username = follower.UserName
            let dateTime =
                DateTime.ParseExact(follower.FollowedAt, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
            where dateTime > since
            select username).ToList();
        return new NewFollowerData
        {
            Followers = list,
            FetchTime = _lastFetch.ToString("yyyy-MM-dd'T'HH.mm.ss'Z'")
        };
    }
}