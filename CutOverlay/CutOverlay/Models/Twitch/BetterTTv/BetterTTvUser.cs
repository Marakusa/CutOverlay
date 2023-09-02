using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch.BetterTTv;

public class BetterTTvUser
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("bots")]
    public List<object> Bots { get; set; }

    [JsonProperty("avatar")]
    public string Avatar { get; set; }

    [JsonProperty("channelEmotes")]
    public List<object> ChannelEmotes { get; set; }

    [JsonProperty("sharedEmotes")]
    public List<BetterTTvEmote> SharedEmotes { get; set; }
}