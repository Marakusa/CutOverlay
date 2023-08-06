using Newtonsoft.Json;

namespace CutOverlay.Models.BeatSaberPlus;

public class BeatSaberPlusWebhookBase
{
    [JsonProperty("_type")] public string? Type { get; set; }
}