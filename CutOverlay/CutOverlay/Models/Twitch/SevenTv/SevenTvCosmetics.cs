using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch.SevenTv;

public class Badge
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("tooltip")]
    public string Tooltip { get; set; }

    [JsonProperty("urls")]
    public List<List<string>> Urls { get; set; }

    [JsonProperty("users")]
    public List<string> Users { get; set; }
}

public class DropShadow
{
    [JsonProperty("x_offset")]
    public double XOffset { get; set; }

    [JsonProperty("y_offset")]
    public double YOffset { get; set; }

    [JsonProperty("radius")]
    public double Radius { get; set; }

    [JsonProperty("color")]
    public int Color { get; set; }
}

public class Paint
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("users")]
    public List<string> Users { get; set; }

    [JsonProperty("function")]
    public string Function { get; set; }

    [JsonProperty("color")]
    public int? Color { get; set; }

    [JsonProperty("stops")]
    public List<Stop> Stops { get; set; }

    [JsonProperty("repeat")]
    public bool Repeat { get; set; }

    [JsonProperty("angle")]
    public int Angle { get; set; }

    [JsonProperty("drop_shadows")]
    public List<DropShadow> DropShadows { get; set; }

    [JsonProperty("shape")]
    public string Shape { get; set; }

    [JsonProperty("image_url")]
    public string ImageUrl { get; set; }
}

public class SevenTvCosmetics
{
    [JsonProperty("t")]
    public long T { get; set; }

    [JsonProperty("badges")]
    public List<Badge> Badges { get; set; }

    [JsonProperty("paints")]
    public List<Paint> Paints { get; set; }
}

public class Stop
{
    [JsonProperty("at")]
    public double At { get; set; }

    [JsonProperty("color")]
    public int Color { get; set; }
}