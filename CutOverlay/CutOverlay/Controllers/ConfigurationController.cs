﻿using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CutOverlay.App.Overlay;
using Microsoft.AspNetCore.Mvc;

namespace CutOverlay.Controllers;

[Route("configuration")]
[ApiController]
public class ConfigurationController : ControllerBase
{
    public static Dictionary<string, string?>? Configurations;
    private readonly IConfiguration _configuration;

    public ConfigurationController(IConfiguration configuration)
    {
        _configuration = configuration;
        Configurations = new Dictionary<string, string?>();
        DecryptAndReadConfig();
    }

    public static string GetConfigFilePath()
    {
        string appDataPath = Globals.GetAppDataPath();
        return Path.Combine(appDataPath, "config.json");
    }

    private void EncryptAndSaveConfig(Dictionary<string, string?> config)
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

    private Dictionary<string, string?>? DecryptAndReadConfig()
    {
        if (!System.IO.File.Exists(GetConfigFilePath()))
            return new Dictionary<string, string?>();

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

        Configurations = JsonSerializer.Deserialize<Dictionary<string, string?>>(decryptedConfigText);
        return Configurations;
    }

    [HttpPost]
    public async Task<IActionResult> SaveConfig([FromBody] Dictionary<string, string?> config)
    {
        try
        {
            bool spotifySettingsRefresh = false;

            if (Configurations != null && config.ContainsKey("spotifyClientId") &&
                Configurations.TryGetValue("spotifyClientId", out string? configuration))
                spotifySettingsRefresh = config["spotifyClientId"] != configuration;

            if (!spotifySettingsRefresh)
                if (Configurations != null && config.ContainsKey("spotifyClientSecret") &&
                    Configurations.TryGetValue("spotifyClientSecret", out string? configuration1))
                    spotifySettingsRefresh = config["spotifyClientSecret"] != configuration1;

            bool twitchSettingsRefresh = false;

            if (Configurations != null && config.ContainsKey("twitchUsername") &&
                Configurations.TryGetValue("twitchUsername", out string? configuration2))
                twitchSettingsRefresh = config["twitchUsername"] != configuration2;
            
            bool pulsoidSettingsRefresh = false;

            if (Configurations != null && config.ContainsKey("pulsoid") &&
                Configurations.TryGetValue("pulsoidAccessToken", out string? configuration3))
                pulsoidSettingsRefresh = config["pulsoidAccessToken"] != configuration3;
            
            EncryptAndSaveConfig(config);
            DecryptAndReadConfig();

            if (spotifySettingsRefresh)
            {
                Spotify.Instance?.Unload();
                Spotify.Instance?.Dispose();
                Spotify.Instance = null;
                _ = new Spotify();
                await Spotify.Instance?.Start(Configurations)!;
            }

            if (twitchSettingsRefresh)
            {
                Twitch.Instance?.Unload();
                Twitch.Instance?.Dispose();
                Twitch.Instance = null;
                _ = new Twitch();
                await Twitch.Instance?.Start(Configurations)!;
            }

            if (pulsoidSettingsRefresh)
            {
                Pulsoid.Instance?.Unload();
                Pulsoid.Instance?.Dispose();
                Pulsoid.Instance = null;
                _ = new Pulsoid();
                await Pulsoid.Instance?.Start(Configurations)!;
            }

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
            Dictionary<string, string?>? config = DecryptAndReadConfig();
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