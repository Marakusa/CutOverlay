using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch.BetterTTv;

public class BetterTTvUser
{
    [JsonProperty("sharedEmotes")]
    public List<BetterTTvEmote> SharedEmotes { get; set; }
}