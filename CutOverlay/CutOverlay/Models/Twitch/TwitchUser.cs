using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch;

public class TwitchUserPaint
{
    [JsonIgnore] public bool IsLocked { get; set; } = false;

    [JsonProperty("backgroundImage")] public string? BackgroundImage { get; set; }

    [JsonProperty("filter")] public string? Filter { get; set; }

    [JsonProperty("backgroundColor")] public string? BackgroundColor { get; set; }

    [JsonProperty("backgroundSize")] public string? BackgroundSize { get; set; }
}

public class TwitchUser
{
    [JsonProperty(PropertyName = "id")] public string Id { get; set; }

    [JsonProperty(PropertyName = "login")] public string Login { get; set; }

    [JsonProperty(PropertyName = "display_name")]
    public string DisplayName { get; set; }

    [JsonProperty(PropertyName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty(PropertyName = "type")] public string Type { get; set; }

    [JsonProperty(PropertyName = "broadcaster_type")]
    public string BroadcasterType { get; set; }

    [JsonProperty(PropertyName = "description")]
    public string Description { get; set; }

    [JsonProperty(PropertyName = "profile_image_url")]
    public string ProfileImageUrl { get; set; }

    [JsonProperty(PropertyName = "offline_image_url")]
    public string OfflineImageUrl { get; set; }

    [JsonProperty(PropertyName = "view_count")]
    public long ViewCount { get; set; }

    [JsonProperty(PropertyName = "email")] public string Email { get; set; }

    [JsonProperty(PropertyName = "color")] public string Color { get; set; }

    [JsonProperty(PropertyName = "color")] public TwitchUserPaint Paint { get; set; }

    [JsonProperty(PropertyName = "sevenTvBadges")]
    public List<string> SevenTvBadges { get; set; }
}