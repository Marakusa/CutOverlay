namespace CutOverlay.Models.BeatSaberPlus.App;

public class BeatSaberAppScoreData
{
    public double Time { get; set; }
    public long Score { get; set; }
    public double Accuracy { get; set; }
    public int Combo { get; set; }
    public int MissCount { get; set; }
    public double CurrentHealth { get; set; }
}