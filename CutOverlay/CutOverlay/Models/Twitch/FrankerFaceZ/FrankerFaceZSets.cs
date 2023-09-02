using Newtonsoft.Json;
using TwitchLib.Api.Helix;

namespace CutOverlay.Models.Twitch.FrankerFaceZ;

public class FrankerFaceZSets
{
    [JsonProperty("sets")]
    public Dictionary<string, Set> Sets { get; set; }
}