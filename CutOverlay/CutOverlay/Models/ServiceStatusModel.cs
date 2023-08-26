using Newtonsoft.Json;

namespace CutOverlay.Models;

public class ServiceStatusModel
{
    [JsonProperty("color")]
    public string? Color { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }
}