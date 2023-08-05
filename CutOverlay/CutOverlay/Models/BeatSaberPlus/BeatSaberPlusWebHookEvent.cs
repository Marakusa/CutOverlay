using Newtonsoft.Json;

namespace CutOverlay.Models.BeatSaberPlus;

public class BeatSaberPlusWebHookEvent : BeatSaberPlusWebhookBase
{
    [JsonProperty("_event")]
    public string? Event { get; set; }
}