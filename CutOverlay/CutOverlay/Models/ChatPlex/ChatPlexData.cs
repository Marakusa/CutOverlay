using Newtonsoft.Json;

namespace CutOverlay.Models.ChatPlex;

public class ChatPlexData
{
    [JsonProperty("Stops")]
    public List<List<object>> Stops { get; set; }

    [JsonProperty("Users")]
    public List<string> Users { get; set; }
}