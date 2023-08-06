using Newtonsoft.Json;

namespace CutOverlay.Models.BeatSaberPlus;

public class BeatSaberPlusWebHookGameState : BeatSaberPlusWebHookEvent
{
    [JsonProperty("gameStateChanged")] public string GameStateChanged { get; set; }
}

public class BeatSaberPlusWebHookResumeTime : BeatSaberPlusWebHookEvent
{
    [JsonProperty("resumeTime")] public double ResumeTime { get; set; }
}

public class BeatSaberPlusWebHookPauseEvent : BeatSaberPlusWebHookEvent
{
    [JsonProperty("pauseTime")] public double PauseTime { get; set; }
}

public class BeatSaberPlusWebHookMapInfoChanged : BeatSaberPlusWebHookEvent
{
    [JsonProperty("mapInfoChanged")] public MapInfoChanged MapInfoChanged { get; set; }
}

public class MapInfoChanged
{
    [JsonProperty("level_id")] public string LevelId { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("sub_name")] public string SubName { get; set; }

    [JsonProperty("artist")] public string Artist { get; set; }

    [JsonProperty("mapper")] public string Mapper { get; set; }

    [JsonProperty("characteristic")] public string Characteristic { get; set; }

    [JsonProperty("difficulty")] public string Difficulty { get; set; }

    [JsonProperty("duration")] public long Duration { get; set; }

    [JsonProperty("BPM")] public double BPM { get; set; }

    [JsonProperty("PP")] public double PP { get; set; }

    [JsonProperty("BSRKey")] public string BSRKey { get; set; }

    [JsonProperty("coverRaw")] public string CoverRaw { get; set; }

    [JsonProperty("time")] public double Time { get; set; }

    [JsonProperty("timeMultiplier")] public double TimeMultiplier { get; set; }
}

public class BeatSaberPlusWebHookScoreEvent : BeatSaberPlusWebHookEvent
{
    [JsonProperty("scoreEvent")] public ScoreEvent ScoreEvent { get; set; }
}

public class ScoreEvent
{
    [JsonProperty("time")] public double Time { get; set; }

    [JsonProperty("score")] public long Score { get; set; }

    [JsonProperty("accuracy")] public double Accuracy { get; set; }

    [JsonProperty("combo")] public int Combo { get; set; }

    [JsonProperty("missCount")] public int MissCount { get; set; }

    [JsonProperty("currentHealth")] public double CurrentHealth { get; set; }
}