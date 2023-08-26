using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch.SevenTv;

public class Data
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("flags")]
    public int Flags { get; set; }

    [JsonProperty("lifecycle")]
    public int Lifecycle { get; set; }

    [JsonProperty("state")]
    public List<string> State { get; set; }

    [JsonProperty("listed")]
    public bool Listed { get; set; }

    [JsonProperty("animated")]
    public bool Animated { get; set; }

    [JsonProperty("owner")]
    public Owner Owner { get; set; }

    [JsonProperty("host")]
    public Host Host { get; set; }

    [JsonProperty("tags")]
    public List<string> Tags { get; set; }
}

public class Emote
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("flags")]
    public int Flags { get; set; }

    [JsonProperty("timestamp")]
    public object Timestamp { get; set; }

    [JsonProperty("actor_id")]
    public string ActorId { get; set; }

    [JsonProperty("data")]
    public Data Data { get; set; }
}

public class File
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("static_name")]
    public string StaticName { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("frame_count")]
    public int FrameCount { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("format")]
    public string Format { get; set; }
}

public class Host
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("files")]
    public List<File> Files { get; set; }
}

public class Owner
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("display_name")]
    public string DisplayName { get; set; }

    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; set; }

    [JsonProperty("style")]
    public Style Style { get; set; }

    [JsonProperty("roles")]
    public List<string> Roles { get; set; }
}

public class SevenTvEmotes
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("flags")]
    public int Flags { get; set; }

    [JsonProperty("tags")]
    public List<object> Tags { get; set; }

    [JsonProperty("immutable")]
    public bool Immutable { get; set; }

    [JsonProperty("privileged")]
    public bool Privileged { get; set; }

    [JsonProperty("emotes")]
    public List<Emote> Emotes { get; set; }

    [JsonProperty("emote_count")]
    public int EmoteCount { get; set; }

    [JsonProperty("capacity")]
    public int Capacity { get; set; }

    [JsonProperty("owner")]
    public Owner Owner { get; set; }
}

public class Style
{
    [JsonProperty("color")]
    public int? Color { get; set; }
}