using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch;

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