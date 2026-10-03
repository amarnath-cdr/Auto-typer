using System;
using System.Collections.Generic;
using System.IO;
using AutoTyper.Models;
using AutoTyper.Services;
using Xunit;

namespace AutoTyper.Tests;

public class StorageServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public StorageServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AutoTyperTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public void ProfileStorage_WhenFileDoesNotExist_ReturnsDefaultsAndCreatesFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "profiles.json");
        var service = new ProfileStorageService(filePath);

        // Act
        var profiles = service.LoadProfiles();

        // Assert
        Assert.NotEmpty(profiles);
        Assert.True(File.Exists(filePath));
        Assert.Contains(profiles, p => p.Name == "Greeting");
    }

    [Fact]
    public void ProfileStorage_SaveAndLoad_RoundTripSuccess()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "profiles.json");
        var service = new ProfileStorageService(filePath);
        var customProfiles = new List<AutoTypeProfile>
        {
            new() { Name = "Custom 1", Shortcut = "F1", Text = "Sample 1" },
            new() { Name = "Custom 2", Shortcut = "F2", Text = "Sample 2" }
        };

        // Act
        service.SaveProfiles(customProfiles);
        var loaded = service.LoadProfiles();

        // Assert
        Assert.Equal(2, loaded.Count);
        Assert.Equal("Custom 1", loaded[0].Name);
        Assert.Equal("Custom 2", loaded[1].Name);
    }

    [Fact]
    public void ProfileStorage_CorruptedJson_RecoversToDefaults()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "corrupted_profiles.json");
        File.WriteAllText(filePath, "{ invalid-json-content: true, [");
        var service = new ProfileStorageService(filePath);

        // Act
        var loaded = service.LoadProfiles();

        // Assert
        Assert.NotEmpty(loaded);
        Assert.Contains(loaded, p => p.Name == "Greeting");
    }

    [Fact]
    public void SettingsStorage_WhenFileDoesNotExist_ReturnsDefaultsAndCreatesFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "settings.json");
        var service = new SettingsStorageService(filePath);

        // Act
        var settings = service.LoadSettings();

        // Assert
        Assert.NotNull(settings);
        Assert.True(File.Exists(filePath));
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.True(settings.AutoTyperMasterEnabled);
    }

    [Fact]
    public void SettingsStorage_SaveAndLoad_PersistsChanges()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "settings.json");
        var service = new SettingsStorageService(filePath);
        var modified = new AppSettings
        {
            Theme = AppTheme.Dark,
            AutoTyperMasterEnabled = false,
            ConfirmOnDelete = false,
            DefaultDelayMs = 120
        };

        // Act
        service.SaveSettings(modified);
        var loaded = service.LoadSettings();

        // Assert
        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.False(loaded.AutoTyperMasterEnabled);
        Assert.False(loaded.ConfirmOnDelete);
        Assert.Equal(120, loaded.DefaultDelayMs);
    }

    [Fact]
    public void SettingsStorage_CorruptedJson_RecoversToDefaults()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "corrupted_settings.json");
        File.WriteAllText(filePath, "CORRUPT DATA <<<");
        var service = new SettingsStorageService(filePath);

        // Act
        var loaded = service.LoadSettings();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(AppTheme.System, loaded.Theme);
        Assert.True(loaded.AutoTyperMasterEnabled);
    }
}
