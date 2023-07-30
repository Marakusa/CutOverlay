namespace CutOverlay.Models;

public class HeartRateModel
{
    public string PulsoidToken
    {
        get
        {
            if (ConfigurationController.Configurations != null && ConfigurationController.Configurations.TryGetValue("pulsoidAccessToken", out string? token))
                return token;
            return "";
        }
    }
}