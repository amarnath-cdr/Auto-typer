using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTyper.Models;
using AutoTyper.Utilities;

namespace AutoTyper.Services;

public class SettingsStorageService : ISettingsStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;
    private readonly object _fileLock = new();

    public SettingsStorageService(string? filePath = null)
    {
        _filePath = filePath ?? PathConstants.SettingsFilePath;
    }

    public AppSettings LoadSettings()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_filePath))
            {
                var defaults = new AppSettings();
                SaveSettings(defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        return await Task.Run(LoadSettings);
    }

    public void SaveSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_fileLock)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await Task.Run(() => SaveSettings(settings));
    }
}
