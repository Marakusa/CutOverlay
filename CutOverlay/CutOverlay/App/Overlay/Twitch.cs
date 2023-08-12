using System.Diagnostics;
using System.Globalization;
using CutOverlay.Models.Twitch;
using Newtonsoft.Json;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;
using TwitchLib.Api.Helix.Models.Chat.Badges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace CutOverlay.App.Overlay;

[Overlay]
public class Twitch : OverlayApp
{
    private const string ClientId = "nrlnctz147p5v5xwy2rhjlas2afh3s";

    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string AuthorizationAddress = "https://id.twitch.tv/oauth2/authorize";
    private const string ResponseType = "token";

    private const string Scopes =
        "chat%3Aread+whispers%3Aread+moderation%3Aread+moderator%3Aread%3Afollowers+user%3Aread%3Aemail";

    private static readonly Random Random =
        new(DateTime.Now.Second + DateTime.Now.DayOfYear + DateTime.Now.Year + DateTime.Now.Millisecond);

    internal static Twitch? Instance;

    private static TwitchAPI? _twitchApi;
    private static FollowerService? _followerService;

    private readonly string _callbackAddress = $"http://localhost:{Globals.Port}/twitch/callback";

    private readonly List<ChannelFollower> _followers = new();

    private readonly TwitchChatWebSocket? _twitchChat;
    private string? _accessToken;
    private Dictionary<string, string?>? _configurations;
    private DateTime _lastFetch = DateTime.UnixEpoch;
    private string _stateToken = "";

    private TwitchClient? _twitchBotClient;
    private List<BadgeEmoteSet> _badges;

    public Twitch()
    {
        if (Instance != null)
        {
            Dispose();
            return;
        }

        Instance = this;

        HttpClient = new HttpClient();
        _twitchChat = new TwitchChatWebSocket();

        _accessToken = null;
        _twitchBotClient = null;
        _twitchApi = null;
        _followerService = null;
    }

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
        _twitchBotClient.OnMessageReceived += OnMessageReceived;

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

        _followerService = new FollowerService(_twitchApi, 5, invokeEventsOnStartup: false);
        _followerService.OnNewFollowersDetected += OnNewFollowers;
        _followerService.OnServiceStarted += OnFollowerServiceStarted;

        GetUsersResponse? users = await _twitchApi.Helix.Users.GetUsersAsync(logins: new List<string>
        {
            _configurations?["twitchUsername"] ?? ""
        });
        _followerService.SetChannelsById(new List<string>
        {
            users.Users[0].Id
        });
        _badges = (await _twitchApi.Helix.Chat.GetChannelChatBadgesAsync(users.Users[0].Id)).EmoteSet.ToList();
        _badges.AddRange((await _twitchApi.Helix.Chat.GetGlobalChatBadgesAsync()).EmoteSet.ToList());

        _followerService.ClearCache();
        _followerService.Start();

        _twitchChat?.Start(_configurations?["twitchUsername"], users.Users[0].Id);
    }

    public override void Unload()
    {
        _twitchChat?.Stop();
        _twitchBotClient?.Disconnect();
        _followerService?.Stop();

        HttpClient?.Dispose();

        _accessToken = null;
        _twitchBotClient = null;
        _twitchApi = null;
        _followerService = null;

        Console.WriteLine("Twitch app unloaded");
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        UserChatMessage message = new()
        {
            Message = e.ChatMessage.Message,
            DisplayName = e.ChatMessage.DisplayName,
            UserColor = e.ChatMessage.ColorHex,
            UserBadges = new List<string>(),
            Flags = new Flags
            {
                Broadcaster = e.ChatMessage.IsBroadcaster,
                Founder = false,
                Highlighted = e.ChatMessage.IsHighlighted,
                Mod = e.ChatMessage.IsModerator,
                Subscriber = e.ChatMessage.IsSubscriber,
                Vip = e.ChatMessage.IsVip
            },
            MessageEmotes = new List<EmoteData>()
        };

        // Set badges
        foreach (BadgeVersion badgeVersion in from badge in e.ChatMessage.Badges
                 let index = _badges.FindIndex(f => f.SetId == badge.Key)
                 where index >= 0
                 select _badges[index].Versions.FirstOrDefault(f => f.Id.ToString() == badge.Value, null)
                 into badgeVersion
                 where badgeVersion?.ImageUrl1x != null
                 select badgeVersion)
        {
            message.UserBadges.Add(badgeVersion.ImageUrl4x);
        }

        // Set emotes
        foreach (EmoteData emoteData in e.ChatMessage.EmoteSet.Emotes.Select(emote => new EmoteData
                 {
                     Url = emote.ImageUrl,
                     StartIndex = emote.StartIndex,
                     EndIndex = emote.EndIndex
                 }))
        {
            emoteData.Url =
                $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteData.Url?.Split("/")[5]}/default/light/2.0";
            message.MessageEmotes.Add(emoteData);
        }

        _ = _twitchChat?.SendMessageToAllAsync(JsonConvert.SerializeObject(message));
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
        Console.WriteLine(
            $"New subscriber alert! User: {e.Subscriber.DisplayName}, Sub Plan: {e.Subscriber.SubscriptionPlan}");
    }

    private void OnRaidNotification(object? sender, OnRaidNotificationArgs e)
    {
        Console.WriteLine(
            $"Raid alert! User: {e.RaidNotification.DisplayName}, Raid Count: {e.RaidNotification.MsgParamViewerCount}");
    }

    private void OnFollowerServiceStarted(object? sender, OnServiceStartedArgs e)
    {
        Console.WriteLine("Follower service started");
    }

    private void OnNewFollowers(object? sender, OnNewFollowersDetectedArgs e)
    {
        foreach (ChannelFollower follower in e.NewFollowers) _followers.Add(follower);

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

    public class UserChatMessage
    {
        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("messageEmotes")]
        public List<EmoteData>? MessageEmotes { get; set; }

        [JsonProperty("userColor")]
        public string? UserColor { get; set; }

        [JsonProperty("userBadges")]
        public List<string>? UserBadges { get; set; }

        [JsonProperty("flags")]
        public Flags? Flags { get; set; }
    }

    public class EmoteData
    {
        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("startIndex")]
        public int StartIndex { get; set; }

        [JsonProperty("endIndex")]
        public int EndIndex { get; set; }
    }

    public class Flags
    {
        [JsonProperty("highlighted")]
        public bool Highlighted { get; set; }

        [JsonProperty("broadcaster")]
        public bool Broadcaster { get; set; }

        [JsonProperty("mod")]
        public bool Mod { get; set; }

        [JsonProperty("vip")]
        public bool Vip { get; set; }

        [JsonProperty("subscriber")]
        public bool Subscriber { get; set; }

        [JsonProperty("founder")]
        public bool Founder { get; set; }
    }
}