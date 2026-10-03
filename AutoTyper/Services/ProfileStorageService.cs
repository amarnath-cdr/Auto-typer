using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTyper.Models;
using AutoTyper.Utilities;

namespace AutoTyper.Services;

public class ProfileStorageService : IProfileStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;
    private readonly object _fileLock = new();

    public ProfileStorageService(string? filePath = null)
    {
        _filePath = filePath ?? PathConstants.ProfilesFilePath;
    }

    public List<AutoTypeProfile> GetDefaultProfiles()
    {
        return
        [
            new AutoTypeProfile
            {
                Name = "Greeting",
                Shortcut = "F7",
                Text = "Hello! Hope you are having a productive day.",
                Comment = "Quick friendly greeting",
                TypingDelayMs = 20,
                TypingMode = TypingMode.Simulated,
                Capitalization = CapitalizationMode.Original,
                RepeatCount = 1,
                IsEnabled = true
            },
            new AutoTypeProfile
            {
                Name = "Email Signature",
                Shortcut = "F8",
                Text = "Best regards,\r\nAutoTyper User\r\nSent via AutoTyper",
                Comment = "Standard closing signature",
                TypingDelayMs = 30,
                TypingMode = TypingMode.Simulated,
                Capitalization = CapitalizationMode.Original,
                RepeatCount = 1,
                IsEnabled = true
            },
            new AutoTypeProfile
            {
                Name = "Code Snippet",
                Shortcut = "F9",
                Text = "// TODO: Implement business logic here\r\nthrow new NotImplementedException();",
                Comment = "C# boilerplate template",
                TypingDelayMs = 15,
                TypingMode = TypingMode.Clipboard,
                Capitalization = CapitalizationMode.Original,
                RepeatCount = 1,
                IsEnabled = true
            }
        ];
    }

    public List<AutoTypeProfile> LoadProfiles()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_filePath))
            {
                var defaults = GetDefaultProfiles();
                SaveProfiles(defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var profiles = JsonSerializer.Deserialize<List<AutoTypeProfile>>(json, JsonOptions);
                return profiles ?? GetDefaultProfiles();
            }
            catch
            {
                // If corrupted, return default profiles
                return GetDefaultProfiles();
            }
        }
    }

    public async Task<List<AutoTypeProfile>> LoadProfilesAsync()
    {
        return await Task.Run(LoadProfiles);
    }

    public void SaveProfiles(IEnumerable<AutoTypeProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        lock (_fileLock)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(profiles, JsonOptions);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
    }

    public async Task SaveProfilesAsync(IEnumerable<AutoTypeProfile> profiles)
    {
        await Task.Run(() => SaveProfiles(profiles));
    }
}
