namespace CutOverlay.Models;

public class HeartRateModel
{
    public string? PulsoidToken => ConfigurationController.Configurations?["pulsoidAccessToken"];
}