using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CutOverlay.Models;
using Microsoft.AspNetCore.Mvc;

namespace CutOverlay;

[Route("configuration")]
[ApiController]
public class ConfigurationController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfigurationController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private static string GetAppDataPath()
    {
        string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CutOverlay");
        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        return appDataPath;
    }

    private static string GetConfigFilePath()
    {
        string appDataPath = GetAppDataPath();
        return Path.Combine(appDataPath, "config.json");
    }

    private void EncryptAndSaveConfig(Dictionary<string, string> config)
    {
        string configText = JsonSerializer.Serialize(config);
        byte[] salt = Encoding.UTF8.GetBytes(_configuration["EncryptionSalt"]);
        byte[] key = new Rfc2898DeriveBytes(_configuration["EncryptionKey"], salt, 10000).GetBytes(32);
        byte[] iv = new Rfc2898DeriveBytes(_configuration["EncryptionIV"], salt, 10000).GetBytes(16);

        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using MemoryStream memoryStream = new();
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        byte[] configData = Encoding.UTF8.GetBytes(configText);
        cryptoStream.Write(configData, 0, configData.Length);
        cryptoStream.FlushFinalBlock();

        System.IO.File.WriteAllBytes(GetConfigFilePath(), memoryStream.ToArray());
    }

    private Dictionary<string, string>? DecryptAndReadConfig()
    {
        if (!System.IO.File.Exists(GetConfigFilePath()))
            return new Dictionary<string, string>();

        byte[] salt = Encoding.UTF8.GetBytes(_configuration["EncryptionSalt"]);
        byte[] key = new Rfc2898DeriveBytes(_configuration["EncryptionKey"], salt, 10000).GetBytes(32);
        byte[] iv = new Rfc2898DeriveBytes(_configuration["EncryptionIV"], salt, 10000).GetBytes(16);

        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        byte[] encryptedConfig = System.IO.File.ReadAllBytes(GetConfigFilePath());
        using MemoryStream memoryStream = new(encryptedConfig);
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader streamReader = new(cryptoStream);
        string decryptedConfigText = streamReader.ReadToEnd();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(decryptedConfigText);
    }

    [HttpPost]
    public IActionResult SaveConfig([FromBody] Dictionary<string, string> config)
    {
        try
        {
            EncryptAndSaveConfig(config);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public IActionResult ReadConfig()
    {
        try
        {
            Dictionary<string, string>? config = DecryptAndReadConfig();
            return Ok(config);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("SpotifyDashboard")]
    public IActionResult SpotifyDashboard()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://developer.spotify.com/dashboard",
                UseShellExecute = true
            });
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("PulsoidDashboard")]
    public IActionResult PulsoidDashboard()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://pulsoid.net/ui/keys",
                UseShellExecute = true
            });
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}