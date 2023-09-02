﻿using Newtonsoft.Json;

namespace CutOverlay.Models.Twitch.BetterTTv;

public class BetterTTvEmote
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("imageType")]
    public string ImageType { get; set; }
}