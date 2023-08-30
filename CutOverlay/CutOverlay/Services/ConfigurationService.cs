using Newtonsoft.Json;

namespace CutOverlay.Services;

public class ConfigurationService
{
    private readonly LoggerService _logger;

    public ConfigurationService(LoggerService logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, string?>?> FetchConfigurationsAsync()
    {
        HttpResponseMessage configurationResponse =
            await new HttpClient().GetAsync($"http://localhost:{Globals.Port}/configuration");

        _logger.LogInformation("Fetching configurations...");
        string configurationJson = await configurationResponse.Content.ReadAsStringAsync();

        if (configurationResponse.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<Dictionary<string, string?>>(configurationJson);
        _logger.LogError($"{configurationJson}");
        return new Dictionary<string, string?>();
    }
}