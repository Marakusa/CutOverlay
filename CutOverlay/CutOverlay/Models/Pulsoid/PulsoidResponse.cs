using Newtonsoft.Json;

namespace CutOverlay.Models;

public class PulsoidResponseData
{
    [JsonProperty("heart_rate")] public int HeartRate { get; set; }
}

public class PulsoidResponse
{
    [JsonProperty("data")] public PulsoidResponseData Data { get; set; }
}