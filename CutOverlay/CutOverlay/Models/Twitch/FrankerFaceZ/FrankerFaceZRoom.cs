using Newtonsoft.Json;
using System.Collections.Generic;

namespace CutOverlay.Models.Twitch.FrankerFaceZ;

public class Set
{
    [JsonProperty("emoticons")]
    public List<FrankerFaceZEmote> Emoticons { get; set; }
}

public class FrankerFaceZEmote
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("width")]
    public int Width { get; set; }
    
    [JsonProperty("height")]
    public int Height { get; set; }
    
    [JsonProperty("urls")]
    public Dictionary<string, string> Urls { get; set; }
}

public class FrankerFaceZRoom
{
    [JsonProperty("sets")]
    public Dictionary<string, Set> Sets { get; set; }
}