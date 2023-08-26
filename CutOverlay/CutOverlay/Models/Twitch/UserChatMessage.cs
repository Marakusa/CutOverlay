using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch;

public class UserChatMessage
{
    [JsonProperty("UserId")]
    public string? UserId { get; set; }

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

    [JsonProperty("userProfileImageUrl")]
    public string? UserProfileImageUrl { get; set; }

    [JsonProperty("flags")]
    public Flags? Flags { get; set; }

    [JsonProperty("paint")]
    public TwitchUserPaint? Paint { get; set; }
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