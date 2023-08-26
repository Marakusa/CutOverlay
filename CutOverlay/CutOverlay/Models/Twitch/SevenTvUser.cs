using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch;

public class Role
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("position")]
    public int Position { get; set; }

    [JsonProperty("color")]
    public int Color { get; set; }

    [JsonProperty("allowed")]
    public int Allowed { get; set; }

    [JsonProperty("denied")]
    public int Denied { get; set; }
}

public class SevenTvUser
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("twitch_id")]
    public string TwitchId { get; set; }

    [JsonProperty("login")]
    public string Login { get; set; }

    [JsonProperty("display_name")]
    public string DisplayName { get; set; }

    [JsonProperty("role")]
    public Role Role { get; set; }
}