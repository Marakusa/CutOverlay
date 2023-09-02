using Newtonsoft.Json;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace CutOverlay.Models.Twitch.SevenTv;

public class SevenTvUserEmotes
{
    [JsonProperty("emote_set")]
    public SevenTvEmotes EmoteSet { get; set; }
}