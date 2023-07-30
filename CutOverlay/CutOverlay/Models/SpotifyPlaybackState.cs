using Newtonsoft.Json;

namespace CutOverlay.Models;

public class Album
{
    [JsonProperty("images")]
    public List<Image>? Images { get; set; }
}

public class Artist
{
    [JsonProperty("name")]
    public string? Name { get; set; }
}

public class Image
{
    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }
}

public class Item
{
    [JsonProperty("album")]
    public Album? Album { get; set; }

    [JsonProperty("artists")]
    public List<Artist>? Artists { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }
}

public class SpotifyPlaybackState
{
    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }

    [JsonProperty("progress_ms")]
    public long ProgressMs { get; set; }

    [JsonProperty("is_playing")]
    public bool IsPlaying { get; set; }

    [JsonProperty("item")]
    public Item? Item { get; set; }

    [JsonProperty("duration_ms")]
    public long DurationMs { get; set; }
}