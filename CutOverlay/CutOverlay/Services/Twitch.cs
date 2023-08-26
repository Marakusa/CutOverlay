using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using CutOverlay.App;
using CutOverlay.Models.Twitch;
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
using Emote = CutOverlay.Models.Twitch.SevenTv.Emote;

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
    private Dictionary<string, string?>? _configurations;
    private SevenTvCosmetics? _cosmetics;
    private DateTime _lastFetch = DateTime.UnixEpoch;
    private List<Emote> _sevenTvEmotes;
    private string _stateToken = "";

    private TwitchClient? _twitchBotClient;

    public Twitch(ConfigurationService configurationService)
    {
        HttpClient = new HttpClient();
        _configurationService = configurationService;

        _accessToken = null;
        _twitchBotClient = null;
        _twitchApi = null;
        _followerService = null;

        _subscribers = new List<string>();

        _sevenTvEmotes = new List<Emote>();

        _userCache.Clear();

        _ = Task.Run(async () => { await Start(await configurationService.FetchConfigurationsAsync()); });
    }

    public async Task RefreshConfigurationsAsync()
    {
        _configurations = await _configurationService.FetchConfigurationsAsync();
    }

    public virtual Task Start(Dictionary<string, string?>? configurations)
    {
        Console.WriteLine("Twitch app starting...");

        _configurations = configurations;

        if (string.IsNullOrEmpty(_configurations?["twitchUsername"]))
            return Task.CompletedTask;

        SetupOAuth(ClientId);

        Console.WriteLine("Twitch app started!");
        return Task.CompletedTask;
    }

    public override void Unload()
    {
        foreach (WebSocket socket in Sockets) socket.Dispose();

        _twitchBotClient?.Disconnect();
        _followerService?.Stop();

        HttpClient?.Dispose();

        _accessToken = null;
        _twitchBotClient = null;
        _twitchApi = null;
        _followerService = null;

        Console.WriteLine("Twitch app unloaded");
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
        if (string.IsNullOrEmpty(state) || !state.Equals(state, StringComparison.InvariantCulture))
            throw new Exception("Failed to verify the request");

        _accessToken = oAuth;

        _cosmetics = await Get7TvCosmeticsAsync();
        _sevenTvEmotes = await Get7TvGlobalEmotesAsync();

        Console.WriteLine($"Loaded {(_cosmetics == null ? 0 : _cosmetics.Badges.Count)} 7TV badges");
        Console.WriteLine($"Loaded {(_cosmetics == null ? 0 : _cosmetics.Paints.Count)} 7TV paints");
        Console.WriteLine($"Loaded {_sevenTvEmotes.Count} 7TV emotes");

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

        _twitchBotClient = new TwitchClient();
        _twitchBotClient.Initialize(credentials, _configurations?["twitchUsername"]);

        _twitchBotClient.OnError += OnError;
        _twitchBotClient.OnJoinedChannel += OnJoinedChannel;
        _twitchBotClient.OnNewSubscriber += OnNewSubscriber;
        _twitchBotClient.OnRaidNotification += OnRaidNotification;
        _twitchBotClient.OnMessageReceived += OnMessageReceived;

        _twitchBotClient.Connect();

        _followerService = new FollowerService(_twitchApi, 5, invokeEventsOnStartup: false);
        _followerService.OnNewFollowersDetected += OnNewFollowers;
        _followerService.OnServiceStarted += OnFollowerServiceStarted;

        _followerService.SetChannelsById(new List<string>
        {
            user.Id
        });
        _badges = (await _twitchApi.Helix.Chat.GetChannelChatBadgesAsync(user.Id)).EmoteSet.ToList();
        _badges.AddRange((await _twitchApi.Helix.Chat.GetGlobalChatBadgesAsync()).EmoteSet.ToList());

        _followerService.ClearCache();
        _followerService.Start();

        _broadcasterId = user.Id;

        _ = StartChatWebSocket();
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

            if (sevenTvUser?.Role.Color != null)
                newUser.Color = Parse7TvColor(sevenTvUser.Role.Color);

            if (sevenTvUser?.TwitchId != null)
            {
                newUser.Paint = Parse7TvPaint(newUser, sevenTvUser.TwitchId);
                newUser.SevenTvBadges = Parse7TvBadges(newUser, sevenTvUser.TwitchId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to fetch 7TV user data: {ex}");
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
            Console.WriteLine($"Failed to fetch 7TV cosmetics: {ex}");
        }

        return null;
    }

    private async Task<List<Emote>> Get7TvGlobalEmotesAsync()
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
            Console.WriteLine($"Failed to fetch 7TV cosmetics: {ex}");
        }

        return new List<Emote>();
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
            Console.WriteLine($"Failed to get 7TV paint for user {twitchId}: {ex}");
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
            Console.WriteLine($"Failed to get 7TV paint for user {twitchId}: {ex}");
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

    #region Twitch Bot Client Events

    private static void OnError(object? sender, OnErrorEventArgs e)
    {
        Console.WriteLine($"Failed to connect: {e.Exception.Message}");
    }

    private static void OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine($"Bot {e.BotUsername} has joined the channel {e.Channel}");
    }

    private static void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {
        // TODO: Sub alerts
        _subscribers.Add(e.Subscriber.DisplayName);
        Console.WriteLine(
            $"New subscriber alert! User: {e.Subscriber.DisplayName}, Sub Plan: {e.Subscriber.SubscriptionPlan}");
    }

    private static void OnRaidNotification(object? sender, OnRaidNotificationArgs e)
    {
        Console.WriteLine(
            $"Raid alert! User: {e.RaidNotification.DisplayName}, Raid Count: {e.RaidNotification.MsgParamViewerCount}");
    }

    private static void OnFollowerServiceStarted(object? sender, OnServiceStartedArgs e)
    {
        Console.WriteLine("Follower service started");
    }

    private void OnNewFollowers(object? sender, OnNewFollowersDetectedArgs e)
    {
        foreach (ChannelFollower follower in e.NewFollowers) _followers.Add(follower);

        _lastFetch = DateTime.UtcNow;
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        _ = Task.Run(async () =>
        {
            TwitchUser? user = _userCache.Find(f => f.DisplayName == e.ChatMessage.DisplayName);
            if (user == null)
            {
                GetUsersResponse? users = await _twitchApi?.Helix.Users.GetUsersAsync(new List<string>
                {
                    e.ChatMessage.UserId
                })!;
                foreach (User u in users.Users) _userCache.Add(await HandleExtensionsAsync(u));
                user = await HandleExtensionsAsync(users.Users.First());
            }

            UserChatMessage message = new()
            {
                UserId = e.ChatMessage.UserId,
                Message = e.ChatMessage.Message,
                DisplayName = e.ChatMessage.DisplayName,
                UserColor = e.ChatMessage.ColorHex,
                Paint = _configurations?["use7TV"] == "true" ? user.Paint : null,
                UserBadges = new List<string>(),
                UserProfileImageUrl = user.ProfileImageUrl,
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
                message.UserBadges.Add(badgeVersion.ImageUrl4x);

            // Add 7TV badges
            if (_configurations?["use7TV"] == "true")
                message.UserBadges.AddRange(user.SevenTvBadges);

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

            // Set 7TV emotes
            if (_configurations?["use7TV"] == "true")
            {
                void AddEmote(int start, int end, string currentWord)
                {
                    int s = start;
                    // Return if any emotes are already added at the start position
                    if (message.MessageEmotes.Find(f => f.StartIndex == s) != null)
                        return;

                    // Find the 7TV emote from the word
                    string word = currentWord;
                    Emote? emote = _sevenTvEmotes.Find(f => f.Name == word || f.Data.Name == word);

                    if (emote != null)
                        message.MessageEmotes.Add(new EmoteData
                        {
                            Url =
                                $"https:{emote.Data.Host.Url}/{emote.Data.Host.Files.FindLast(f => f.Format.Equals("AVIF", StringComparison.InvariantCultureIgnoreCase))?.Name}",
                            StartIndex = start,
                            EndIndex = end,
                            Overlay = IsOverlayEmote(emote.Data.Flags)
                        });
                }

                int i = 0;
                int start = 0;
                string currentWord = "";
                while (i < e.ChatMessage.Message.Length)
                {
                    if (e.ChatMessage.Message[i] == ' ')
                    {
                        AddEmote(start, i - 1, currentWord);

                        start = i + 1;
                        currentWord = "";
                    }
                    else
                    {
                        currentWord += e.ChatMessage.Message[i];
                    }

                    i++;
                }

                AddEmote(start, i - 1, currentWord);
            }

            // Sort emotes
            message.MessageEmotes = message.MessageEmotes.OrderBy(o => o.StartIndex).ToList();

            await SendMessageToAllAsync(JsonConvert.SerializeObject(message));
        });
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

    private async void ProcessWebSocketRequest(HttpListenerContext context)
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
            await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
    }

    #endregion

    //public void DebugMessages()
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        UserChatMessage message = new()
    //        {
    //            Message = "This is a test message",
    //            DisplayName = "Marakusa",
    //            UserColor = "",
    //            UserBadges = new List<string>(),
    //            UserProfileImageUrl = user.ProfileImageUrl,
    //            Flags = new Flags
    //            {
    //                Broadcaster = e.ChatMessage.IsBroadcaster,
    //                Founder = false,
    //                Highlighted = e.ChatMessage.IsHighlighted,
    //                Mod = e.ChatMessage.IsModerator,
    //                Subscriber = e.ChatMessage.IsSubscriber,
    //                Vip = e.ChatMessage.IsVip
    //            },
    //            MessageEmotes = new List<EmoteData>()
    //        };

    //        // Set badges
    //        foreach (BadgeVersion badgeVersion in from badge in e.ChatMessage.Badges
    //                                              let index = _badges.FindIndex(f => f.SetId == badge.Key)
    //                                              where index >= 0
    //                                              select _badges[index].Versions.FirstOrDefault(f => f.Id.ToString() == badge.Value, null)
    //                 into badgeVersion
    //                                              where badgeVersion?.ImageUrl1x != null
    //                                              select badgeVersion)
    //        {
    //            message.UserBadges.Add(badgeVersion.ImageUrl4x);
    //        }

    //        // Set emotes
    //        foreach (EmoteData emoteData in e.ChatMessage.EmoteSet.Emotes.Select(emote => new EmoteData
    //        {
    //            Url = emote.ImageUrl,
    //            StartIndex = emote.StartIndex,
    //            EndIndex = emote.EndIndex
    //        }))
    //        {
    //            emoteData.Url =
    //                $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteData.Url?.Split("/")[5]}/default/light/2.0";
    //            message.MessageEmotes.Add(emoteData);
    //        }

    //        await SendMessageToAllAsync(JsonConvert.SerializeObject(message));
    //    });
    //}
}