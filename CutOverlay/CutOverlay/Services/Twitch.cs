using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using CutOverlay.App;
using CutOverlay.Models.Twitch;
using CutOverlay.Models.Twitch.BetterTTv;
using CutOverlay.Models.Twitch.FrankerFaceZ;
using CutOverlay.Models.Twitch.SevenTv;
using Newtonsoft.Json;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;
using TwitchLib.Api.Helix.Models.Chat.Badges;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace CutOverlay.Services;

public class Twitch : OverlayApp
{
    private const string ClientId = "nrlnctz147p5v5xwy2rhjlas2afh3s";

    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string AuthorizationAddress = "https://id.twitch.tv/oauth2/authorize";
    private const string ResponseType = "token";

    private const string Scopes =
        "chat%3Aread+whispers%3Aread+moderation%3Aread+moderator%3Aread%3Afollowers+user%3Aread%3Aemail+channel%3Aread%3Asubscriptions";

    private static readonly List<WebSocket> Sockets = new();

    private static readonly Random Random =
        new(DateTime.Now.Second + DateTime.Now.DayOfYear + DateTime.Now.Year + DateTime.Now.Millisecond);

    private static TwitchAPI? _twitchApi;
    private static FollowerService? _followerService;
    private static List<string> _subscribers = new();

    private readonly string _callbackAddress = $"http://localhost:{Globals.Port}/twitch/callback";
    private readonly ConfigurationService _configurationService;

    private readonly List<ChannelFollower> _followers = new();

    private readonly List<TwitchUser> _userCache = new();

    private string? _accessToken;
    private List<BadgeEmoteSet>? _badges;

    private string? _broadcasterId;
    private string? _chatLogin;
    private Dictionary<string, string?>? _configurations;
    private SevenTvCosmetics? _cosmetics;
    private DateTime _lastFetch = DateTime.UnixEpoch;
    private List<SevenTvEmote> _sevenTvEmotes;
    private List<SevenTvEmote> _sevenTvChannelEmotes;
    private List<BetterTTvEmote> _betterTTvEmotes;
    private List<BetterTTvEmote> _betterTTvChannelEmotes;
    private List<FrankerFaceZEmote> _frankerFaceZEmotes;
    private List<FrankerFaceZEmote> _frankerFaceZChannelEmotes;
    private string _stateToken = "";

    private TwitchClient? _twitchBotClient;
    private readonly LoggerService _logger;

    public Twitch(ConfigurationService configurationService, LoggerService logger)
    {
        Status = ServiceStatusType.Starting;

        HttpClient = new HttpClient();
        _configurationService = configurationService;
        _logger = logger;

        _accessToken = null;
        _twitchApi = null;
        _followerService = null;

        _subscribers = new List<string>();

        _sevenTvEmotes = new List<SevenTvEmote>();
        _sevenTvChannelEmotes = new List<SevenTvEmote>();
        _betterTTvEmotes = new List<BetterTTvEmote>();
        _betterTTvChannelEmotes = new List<BetterTTvEmote>();
        _frankerFaceZEmotes = new List<FrankerFaceZEmote>();
        _frankerFaceZChannelEmotes = new List<FrankerFaceZEmote>();

        SetupChatBot();

        _userCache.Clear();

        _ = Task.Run(async () => { await Start(await configurationService.FetchConfigurationsAsync()); });
    }

    private void SetupChatBot()
    {
        _twitchBotClient = new TwitchClient();
        _twitchBotClient.OnLog += (_, args) =>
            _logger.LogTrace($"{args.DateTime} {args.BotUsername}: {args.Data}");
        _twitchBotClient.OnError += OnError;
        _twitchBotClient.OnJoinedChannel += OnJoinedChannel;
        _twitchBotClient.OnNewSubscriber += OnNewSubscriber;
        _twitchBotClient.OnRaidNotification += OnRaidNotification;
        _twitchBotClient.OnMessageReceived += OnMessageReceived;
    }

    public virtual Task Start(Dictionary<string, string?>? configurations)
    {
        _logger.LogInformation("Twitch app starting...");

        _configurations = configurations;

        SetupOAuth(ClientId);

        _logger.LogInformation("Twitch app started!");
        return Task.CompletedTask;
    }

    public override void Unload()
    {
        Status = ServiceStatusType.Stopping;

        foreach (WebSocket socket in Sockets) socket.Dispose();
        
        _followerService?.Stop();

        HttpClient?.Dispose();

        _accessToken = null;
        _twitchApi = null;
        _followerService = null;

        _logger.LogInformation("Twitch app unloaded");

        Status = ServiceStatusType.Stopped;
    }

    public override ServiceStatusType GetStatus()
    {
        return Status;
    }

    public async Task RefreshConfigurationsAsync()
    {
        _configurations = await _configurationService.FetchConfigurationsAsync();
    }

    #region Service Methods

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
        try
        {
            if (string.IsNullOrEmpty(state) || !state.Equals(state, StringComparison.InvariantCulture))
                throw new Exception("Failed to verify the request");

            _accessToken = oAuth;
            
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
            GetUsersResponse? users = await _twitchApi.Helix.Users.GetUsersAsync();
            foreach (User u in users.Users) _userCache.Add(await HandleExtensionsAsync(u));
            User? user = users.Users.First();

            ConnectionCredentials credentials = new(user.Login, $"oauth:{oAuth}");

            _broadcasterId = user.Id;
            _chatLogin = user.Login;

            _twitchBotClient!.Initialize(credentials, _chatLogin);
            _twitchBotClient.Connect();

            await StartFollowerServiceAsync(user);

            _ = StartChatWebSocket();
        }
        catch
        {
            Status = ServiceStatusType.Error;
            throw;
        }
    }

    private async Task StartFollowerServiceAsync(User user)
    {
        async Task SetupServiceAsync()
        {
            _followerService = new FollowerService(_twitchApi, 5, invokeEventsOnStartup: false);
            _followerService.OnServiceStopped += async (_, _) => await SetupServiceAsync();
            _followerService.OnNewFollowersDetected += OnNewFollowers;
            _followerService.OnServiceStarted += OnFollowerServiceStarted;

            _followerService.SetChannelsById(new List<string>
            {
                user.Id
            });
            _badges = (await _twitchApi?.Helix.Chat.GetChannelChatBadgesAsync(user.Id)!).EmoteSet.ToList();
            _badges.AddRange((await _twitchApi.Helix.Chat.GetGlobalChatBadgesAsync()).EmoteSet.ToList());

            _followerService.ClearCache();
            _followerService.Start();
        }
        
        if (_followerService == null)
            await SetupServiceAsync();
        else
            _followerService.Stop();
    }

    private async Task LoadIntegrationsDataAsync()
    {
        // 7TV stuff
        _cosmetics = await Get7TvCosmeticsAsync();
        _sevenTvEmotes = await Get7TvGlobalEmotesAsync();
        _sevenTvChannelEmotes = await Get7TvChannelEmotesAsync();

        // BetterTTV stuff
        _betterTTvEmotes = await GetBetterTTvGlobalEmotesAsync();
        _betterTTvChannelEmotes = await GetBetterTTvChannelEmotesAsync();

        // BetterTTV stuff
        _frankerFaceZEmotes = await GetFrankerFaceZGlobalEmotesAsync();
        _frankerFaceZChannelEmotes = await GetFrankerFaceZChannelEmotesAsync();

        _logger.LogInformation($"Loaded {(_cosmetics == null ? 0 : _cosmetics.Badges.Count)} 7TV badges");
        _logger.LogInformation($"Loaded {(_cosmetics == null ? 0 : _cosmetics.Paints.Count)} 7TV paints");
        _logger.LogInformation($"Loaded {_sevenTvEmotes.Count} 7TV global emotes");
        _logger.LogInformation($"Loaded {_sevenTvChannelEmotes.Count} 7TV channel emotes");
        _logger.LogInformation($"Loaded {_betterTTvEmotes.Count} BetterTTV global emotes");
        _logger.LogInformation($"Loaded {_betterTTvChannelEmotes.Count} BetterTTV channel emotes");
        _logger.LogInformation($"Loaded {_frankerFaceZEmotes.Count} FrankerFaceZ global emotes");
        _logger.LogInformation($"Loaded {_frankerFaceZChannelEmotes.Count} FrankerFaceZ channel emotes");
    }

    private async Task<TwitchUser> HandleExtensionsAsync(User user)
    {
        TwitchUser newUser = new()
        {
            Id = user.Id,
            Login = user.Login,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            Type = user.Type,
            BroadcasterType = user.BroadcasterType,
            Description = user.Description,
            ProfileImageUrl = user.ProfileImageUrl,
            OfflineImageUrl = user.OfflineImageUrl,
            ViewCount = user.ViewCount,
            Email = user.Email,
            Color = "#fff",
            Paint = new TwitchUserPaint(),
            SevenTvBadges = new List<string>()
        };

        try
        {
            const string userApiUri = "https://7tv.io/v2/users/{0}";

            HttpResponseMessage response = await HttpClient?.GetAsync(string.Format(userApiUri, user.Id))!;

            string content = await response.Content.ReadAsStringAsync();
            SevenTvUser? sevenTvUser = JsonConvert.DeserializeObject<SevenTvUser>(content);

            if (sevenTvUser?.Role?.Color != null)
                newUser.Color = Parse7TvColor(sevenTvUser.Role.Color);

            if (sevenTvUser?.TwitchId != null)
            {
                newUser.Paint = Parse7TvPaint(newUser, sevenTvUser.TwitchId);
                newUser.SevenTvBadges = Parse7TvBadges(newUser, sevenTvUser.TwitchId);
            }
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to fetch 7TV user data: {ex}");
        }

        return newUser;
    }

    #endregion

    #region 7TV

    private async Task<SevenTvCosmetics?> Get7TvCosmeticsAsync()
    {
        try
        {
            const string cosmeticsApiUri = "https://7tv.io/v2/cosmetics?user_identifier=twitch_id";

            HttpResponseMessage response = await HttpClient?.GetAsync(cosmeticsApiUri)!;

            string content = await response.Content.ReadAsStringAsync();
            SevenTvCosmetics? cosmetics = JsonConvert.DeserializeObject<SevenTvCosmetics>(content);

            return cosmetics ?? null;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to fetch 7TV cosmetics: {ex}");
        }

        return null;
    }

    private async Task<List<SevenTvEmote>> Get7TvGlobalEmotesAsync()
    {
        try
        {
            const string emotesApiUri = "https://7tv.io/v3/emote-sets/global";

            HttpResponseMessage response = await HttpClient?.GetAsync(emotesApiUri)!;

            string content = await response.Content.ReadAsStringAsync();
            SevenTvEmotes? globalEmotes = JsonConvert.DeserializeObject<SevenTvEmotes>(content);

            if (globalEmotes != null) return globalEmotes.Emotes;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to fetch 7TV emotes: {ex}");
        }

        return new List<SevenTvEmote>();
    }

    private async Task<List<SevenTvEmote>> Get7TvChannelEmotesAsync()
    {
        try
        {
            long timestampMilliseconds = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalMilliseconds;
            string emotesApiUri = $"https://7tv.io/v3/users/twitch/{_broadcasterId}?cache={timestampMilliseconds}";

            HttpResponseMessage response = await HttpClient?.GetAsync(emotesApiUri)!;

            string content = await response.Content.ReadAsStringAsync();
            SevenTvUserEmotes? user = JsonConvert.DeserializeObject<SevenTvUserEmotes>(content);

            if (user != null) return user.EmoteSet.Emotes;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to fetch 7TV emotes: {ex}");
        }

        return new List<SevenTvEmote>();
    }

    private TwitchUserPaint Parse7TvPaint(TwitchUser twitchUser, string twitchId)
    {
        try
        {
            TwitchUserPaint paint = twitchUser.Paint;

            Paint? cosmetic = _cosmetics?.Paints.Find(f => f.Users.FindIndex(u => u == twitchId) > -1);

            if (cosmetic == null)
                return paint;

            if (cosmetic.Function == "url")
            {
                paint.BackgroundImage = $"url({cosmetic.ImageUrl})";
            }
            else
            {
                List<string> stops = cosmetic.Stops.Select(stop => $"{Parse7TvColor(stop.Color)} {stop.At * 100}%")
                    .ToList();

                string func = cosmetic.Function;
                if (cosmetic.Repeat) func = $"repeating-{cosmetic.Function}";

                string angle = $"{cosmetic.Angle}deg";
                if (cosmetic.Function == "radial-gradient") angle = $"{cosmetic.Shape} at {cosmetic.Angle}%";

                paint.BackgroundImage = $"{func}({angle}, {string.Join(',', stops)})";
            }

            if (cosmetic.Color != null) paint.BackgroundColor = Parse7TvColor((int)cosmetic.Color);

            paint.BackgroundSize = "contain";
            return paint;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to get 7TV paint for user {twitchId}: {ex}");
        }

        return twitchUser.Paint;
    }

    private List<string> Parse7TvBadges(TwitchUser twitchUser, string twitchId)
    {
        try
        {
            List<string> badges = twitchUser.SevenTvBadges;
            List<Badge>? badgeCosmetics = _cosmetics?.Badges.FindAll(f => f.Users.FindIndex(u => u == twitchId) > -1);

            if (badgeCosmetics == null)
                return badges;

            badges.AddRange(badgeCosmetics.Select(badge => badge.Urls.Last().Last()));

            return badges;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to get 7TV paint for user {twitchId}: {ex}");
        }

        return twitchUser.SevenTvBadges;
    }

    private static string Parse7TvColor(int color)
    {
        if (color == 0) return "#fff";

        int red = (color >> 24) & 0xFF;
        int green = (color >> 16) & 0xFF;
        int blue = (color >> 8) & 0xFF;

        return $"rgba({red}, {green}, {blue}, 1)";
    }

    #endregion

    #region BetterTTV

    private async Task<List<BetterTTvEmote>> GetBetterTTvGlobalEmotesAsync()
    {
        try
        {
            const string emotesApiUri = "https://api.betterttv.net/3/cached/emotes/global";

            HttpResponseMessage response = await HttpClient?.GetAsync(emotesApiUri)!;

            string content = await response.Content.ReadAsStringAsync();
            var globalEmotes = JsonConvert.DeserializeObject<List<BetterTTvEmote>>(content);

            if (globalEmotes != null) return globalEmotes;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to fetch BetterTTV emotes: {ex}");
        }

        return new List<BetterTTvEmote>();
    }

    private async Task<List<BetterTTvEmote>> GetBetterTTvChannelEmotesAsync()
    {
        try
        {
            long timestampMilliseconds = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalMilliseconds;
            string emotesApiUri = $"https://api.betterttv.net/3/cached/users/twitch/{_broadcasterId}?cache={timestampMilliseconds}";
            
            HttpResponseMessage response = await HttpClient?.GetAsync(emotesApiUri)!;

            string content = await response.Content.ReadAsStringAsync();
            BetterTTvUser? user = JsonConvert.DeserializeObject<BetterTTvUser> (content);

            if (user != null) return user.SharedEmotes;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to fetch BetterTTV emotes: {ex}");
        }

        return new List<BetterTTvEmote>();
    }

    #endregion

    #region FrankerFaceZ

    private async Task<List<FrankerFaceZEmote>> GetFrankerFaceZGlobalEmotesAsync()
    {
        var output = new List<FrankerFaceZEmote>();

        try
        {
            const string emotesApiUri = "https://api.frankerfacez.com/v1/set/global";

            HttpResponseMessage response = await HttpClient?.GetAsync(emotesApiUri)!;

            string content = await response.Content.ReadAsStringAsync();
            FrankerFaceZSets? sets = JsonConvert.DeserializeObject<FrankerFaceZSets>(content);

            if (sets == null) return output;

            foreach ((string _, Set? set) in sets.Sets) output.AddRange(set.Emoticons);

            return output;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to fetch FrankerFaceZ emotes: {ex}");
        }

        return output;
    }

    private async Task<List<FrankerFaceZEmote>> GetFrankerFaceZChannelEmotesAsync()
    {
        var output = new List<FrankerFaceZEmote>();

        try
        {
            long timestampMilliseconds = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalMilliseconds;
            string emotesApiUri = $"https://api.frankerfacez.com/v1/room/id/{_broadcasterId}?cache={timestampMilliseconds}";

            HttpResponseMessage response = await HttpClient?.GetAsync(emotesApiUri)!;

            string content = await response.Content.ReadAsStringAsync();
            FrankerFaceZRoom? room = JsonConvert.DeserializeObject<FrankerFaceZRoom>(content);

            if (room == null) return output;

            foreach ((string _, Set? set) in room.Sets) output.AddRange(set.Emoticons);

            return output;
        }
        catch (Exception ex)
        {
            Status = ServiceStatusType.Error;
            _logger.LogError($"Failed to fetch FrankerFaceZ emotes: {ex}");
        }

        return output;
    }

    #endregion

    #region Twitch Bot Client Events

    private void OnError(object? sender, OnErrorEventArgs e)
    {
        _logger.LogError($"Failed to connect: {e.Exception.Message}");
    }

    private void OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        _logger.LogInformation($"Bot {e.BotUsername} has joined the channel {e.Channel}");
    }

    private void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {
        // TODO: Sub alerts
        _subscribers.Add(e.Subscriber.DisplayName);
        _logger.LogDebug(
            $"New subscriber alert! User: {e.Subscriber.DisplayName}, Sub Plan: {e.Subscriber.SubscriptionPlan}");
    }

    private void OnRaidNotification(object? sender, OnRaidNotificationArgs e)
    {
        _logger.LogDebug(
            $"Raid alert! User: {e.RaidNotification.DisplayName}, Raid Count: {e.RaidNotification.MsgParamViewerCount}");
    }

    private void OnFollowerServiceStarted(object? sender, OnServiceStartedArgs e)
    {
        _logger.LogInformation("Follower service started");
    }

    private void OnNewFollowers(object? sender, OnNewFollowersDetectedArgs e)
    {
        foreach (ChannelFollower follower in e.NewFollowers) _followers.Add(follower);

        _lastFetch = DateTime.UtcNow;
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        _ = Task.Run(async () => await ReceiveMessageAsync(e.ChatMessage));
    }

    #endregion

    #region Api Calls

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

    public async Task<string> GetLatestFollowerAsync()
    {
        GetChannelFollowersResponse? response =
            await _twitchApi?.Helix.Channels.GetChannelFollowersAsync(_broadcasterId, first: 1)!;
        if (response?.Data == null || response.Data.Length == 0)
            return "";
        return response.Data.First().UserName;
    }

    public async Task<string> GetLatestSubscriberAsync()
    {
        if (_subscribers.Count != 0) return _subscribers.Last();
        bool done = false;
        string? lastSub = null;
        string? pagination = null;
        while (!done)
        {
            GetBroadcasterSubscriptionsResponse? response =
                await _twitchApi?.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(_broadcasterId, 2,
                    pagination)!;
            if (response?.Data == null || response.Data.Length == 0)
            {
                done = true;
                continue;
            }

            foreach (Subscription subscription in response.Data)
                if (subscription.UserId != _broadcasterId)
                    _subscribers.Add(subscription.UserName);

            lastSub = _subscribers.Last();
            pagination = response.Pagination.Cursor;
        }

        return lastSub ?? "";
    }

    #endregion

    #region Chat Web Socket

    private async Task StartChatWebSocket()
    {
        HttpListener listener = new();
        listener.Prefixes.Add(
            $"http://localhost:{Globals.ChatWebSocketPort}/"); // Replace with your desired address

        listener.Start();
        _logger.LogInformation("Listening for WebSocket connections...");
        Status = ServiceStatusType.Running;

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

        _logger.LogInformation("WebSocket connection established.");

        await LoadIntegrationsDataAsync();

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
        if (_configurations?["twitchChat"] != "true")
            return;

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

    #endregion

    #region Private Methods

    private async Task ReceiveMessageAsync(ChatMessage chatMessage)
    {
        _logger.LogDebug($"New message from {chatMessage.DisplayName}: {chatMessage.Message}");

        TwitchUser? user = _userCache.Find(f => f.DisplayName == chatMessage.DisplayName);
        if (user == null)
        {
            GetUsersResponse? users = await _twitchApi?.Helix.Users.GetUsersAsync(new List<string>
                {
                    chatMessage.UserId
                })!;
            foreach (User u in users.Users) _userCache.Add(await HandleExtensionsAsync(u));
            user = await HandleExtensionsAsync(users.Users.First());
        }
        
        UserChatMessage message = new()
        {
            UserId = chatMessage.UserId,
            Message = chatMessage.Message,
            DisplayName = chatMessage.DisplayName,
            UserColor = chatMessage.ColorHex,
            Paint = _configurations?["use7TV"] == "true" && _configurations?["use7TVpaints"] == "true" ? user.Paint : null,
            UserBadges = new List<string>(),
            UserProfileImageUrl = user.ProfileImageUrl,
            Flags = new Flags
            {
                Broadcaster = chatMessage.IsBroadcaster,
                Founder = false,
                Highlighted = chatMessage.IsHighlighted,
                Mod = chatMessage.IsModerator,
                Subscriber = chatMessage.IsSubscriber,
                Vip = chatMessage.IsVip
            },
            MessageEmotes = new List<EmoteData>()
        };
        
        // Set badges
        foreach (BadgeVersion badgeVersion in from badge in chatMessage.Badges
                                              let index = _badges.FindIndex(f => f.SetId == badge.Key)
                                              where index >= 0
                                              select _badges[index].Versions.FirstOrDefault(f => f.Id.ToString() == badge.Value, null)
                 into badgeVersion
                                              where badgeVersion?.ImageUrl1x != null
                                              select badgeVersion)
            message.UserBadges.Add(badgeVersion.ImageUrl4x);
        
        // Add 7TV badges
        if (_configurations?["use7TV"] == "true" && _configurations?["use7TVbadges"] == "true")
            message.UserBadges.AddRange(user.SevenTvBadges);
        
        // Set emotes
        foreach (EmoteData emoteData in chatMessage.EmoteSet.Emotes.Select(emote => new EmoteData
        {
            Url = emote.ImageUrl,
            StartIndex = emote.StartIndex,
            EndIndex = emote.EndIndex,
            AspectRatio = 1
        }))
        {
            emoteData.Url =
                $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteData.Url?.Split("/")[5]}/default/light/2.0";
            message.MessageEmotes.Add(emoteData);
        }
        
        // Set 7TV emotes
        if (_configurations?["use7TV"] == "true")
        {
            void AddEmote(int start, int end, string currentWord)
            {
                try
                {
                    int s = start;
                    // Return if any emotes are already added at the start position
                    if (message.MessageEmotes.Find(f => f.StartIndex == s) != null)
                        return;

                    // Find the 7TV emote from the word
                    string word = currentWord;
                    SevenTvEmote? emote = null;

                    if (_configurations?["use7TVglobalEmotes"] == "true")
                    {
                        emote = _sevenTvEmotes.Find(f => f.Name == word || f.Data.Name == word);
                    }

                    if (_configurations?["use7TVchannelEmotes"] == "true")
                    {
                        emote = _sevenTvChannelEmotes.Find(f => f.Name == word || f.Data.Name == word) ?? emote;
                    }

                    if (emote != null)
                        message.MessageEmotes.Add(new EmoteData
                        {
                            Url =
                                $"https:{emote.Data.Host.Url}/{emote.Data.Host.Files.FindLast(f => f.Format.Equals("AVIF", StringComparison.InvariantCultureIgnoreCase))?.Name}",
                            StartIndex = start,
                            EndIndex = end,
                            Overlay = IsOverlayEmote(emote.Data.Flags),
                            AspectRatio = emote.Data.Host.Files.Last().Width / emote.Data.Host.Files.Last().Height
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                }
            }

            try
            {
                int i = 0;
                int start = 0;
                string currentWord = "";
                while (i < chatMessage.Message.Length)
                {
                    if (chatMessage.Message[i] == ' ')
                    {
                        AddEmote(start, i - 1, currentWord);

                        start = i + 1;
                        currentWord = "";
                    }
                    else
                    {
                        currentWord += chatMessage.Message[i];
                    }

                    i++;
                }

                AddEmote(start, i - 1, currentWord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }
        
        // Set BetterTTV emotes
        if (_configurations?["useBetterTTV"] == "true")
        {
            void AddBetterTTvEmote(int start, int end, string currentWord)
            {
                try
                {
                    int s = start;
                    // Return if any emotes are already added at the start position
                    if (message.MessageEmotes.Find(f => f.StartIndex == s) != null)
                        return;

                    // Find the 7TV emote from the word
                    string word = currentWord;

                    BetterTTvEmote? emote = null;

                    if (_configurations?["useBetterTTVglobalEmotes"] == "true")
                    {
                        emote = _betterTTvEmotes.Find(f => f.Code == word);
                    }

                    if (_configurations?["useBetterTTVchannelEmotes"] == "true")
                    {
                        emote = _betterTTvChannelEmotes.Find(f => f.Code == word) ?? emote;
                    }

                    if (emote != null)
                        message.MessageEmotes.Add(new EmoteData
                        {
                            Url = $"https://cdn.betterttv.net/emote/{emote.Id}/3x.{emote.ImageType}",
                            StartIndex = start,
                            EndIndex = end,
                            Overlay = false,
                            AspectRatio = 1
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                }
            }

            try
            {
                int i = 0;
                int start = 0;
                string currentWord = "";
                while (i < chatMessage.Message.Length)
                {
                    if (chatMessage.Message[i] == ' ')
                    {
                        AddBetterTTvEmote(start, i - 1, currentWord);

                        start = i + 1;
                        currentWord = "";
                    }
                    else
                    {
                        currentWord += chatMessage.Message[i];
                    }

                    i++;
                }

                AddBetterTTvEmote(start, i - 1, currentWord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        // Set FrankerFaceZ emotes
        if (_configurations?["useFrankerFaceZ"] == "true")
        {
            void AddFrankerFaceZEmote(int start, int end, string currentWord)
            {
                try
                {
                    int s = start;
                    // Return if any emotes are already added at the start position
                    if (message.MessageEmotes.Find(f => f.StartIndex == s) != null)
                        return;

                    // Find the 7TV emote from the word
                    string word = currentWord;

                    FrankerFaceZEmote? emote = null;

                    if (_configurations?["useFrankerFaceZglobalEmotes"] == "true")
                    {
                        emote = _frankerFaceZEmotes.Find(f => f.Name == word);
                    }

                    if (_configurations?["useFrankerFaceZchannelEmotes"] == "true")
                    {
                        emote = _frankerFaceZChannelEmotes.Find(f => f.Name == word) ?? emote;
                    }

                    if (emote != null)
                        message.MessageEmotes.Add(new EmoteData
                        {
                            Url = emote.Urls.Last().Value,
                            StartIndex = start,
                            EndIndex = end,
                            Overlay = false,
                            AspectRatio = emote.Width / emote.Height
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                }
            }

            try
            {
                int i = 0;
                int start = 0;
                string currentWord = "";
                while (i < chatMessage.Message.Length)
                {
                    if (chatMessage.Message[i] == ' ')
                    {
                        AddFrankerFaceZEmote(start, i - 1, currentWord);

                        start = i + 1;
                        currentWord = "";
                    }
                    else
                    {
                        currentWord += chatMessage.Message[i];
                    }

                    i++;
                }

                AddFrankerFaceZEmote(start, i - 1, currentWord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        // Sort emotes
        message.MessageEmotes = message.MessageEmotes.OrderBy(o => o.StartIndex).ToList();
        
        await SendMessageToAllAsync(JsonConvert.SerializeObject(message));
    }

    private static bool IsOverlayEmote(int? flags, bool isLegacy = false)
    {
        return flags != null && HasFlag((int)flags, isLegacy ? 1 << 7 : 1 << 8);
    }

    private static bool HasFlag(int flags, int flag)
    {
        return (flags & flag) != 0;
    }

    #endregion
}