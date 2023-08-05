using Newtonsoft.Json;

namespace CutOverlay.Models.BeatSaberPlus.BeatSaver;

public class BeatSaverMapData
{
    [JsonProperty("versions")]
    public List<BeatSaverMapDataVersion>? Versions { get; set; }
}

public class BeatSaverMapDataVersion
{
    [JsonProperty("coverURL")]
    public string? CoverUrl { get; set; }
}