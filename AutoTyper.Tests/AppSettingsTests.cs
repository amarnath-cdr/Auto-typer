using System;
using System.IO;
using System.Text.Json;
using AutoTyper.Models;
using AutoTyper.Services;
using Xunit;

namespace AutoTyper.Tests;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_Defaults_AreSafeAndExpected()
    {
        var settings = new AppSettings();

        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.True(settings.AutoTyperMasterEnabled);
        Assert.True(settings.ConfirmOnDelete);
        Assert.Equal(20, settings.DefaultDelayMs);
        Assert.Equal("Escape", settings.GlobalStopHotkey);
        Assert.True(settings.MinimizeToTray);
        Assert.True(settings.CloseToTray);
        Assert.False(settings.StartWithWindows);
        Assert.True(settings.ShowNotifications);
    }

    [Fact]
    public void AppSettings_Clone_CreatesIndependentCopy()
    {
        var original = new AppSettings
        {
            Theme = AppTheme.Dark,
            AutoTyperMasterEnabled = false,
            ConfirmOnDelete = false,
            DefaultDelayMs = 55,
            GlobalStopHotkey = "Ctrl+Alt+Escape",
            MinimizeToTray = false,
            CloseToTray = false,
            StartWithWindows = true,
            ShowNotifications = false
        };

        var clone = original.Clone();

        Assert.Equal(original.Theme, clone.Theme);
        Assert.Equal(original.AutoTyperMasterEnabled, clone.AutoTyperMasterEnabled);
        Assert.Equal(original.ConfirmOnDelete, clone.ConfirmOnDelete);
        Assert.Equal(original.DefaultDelayMs, clone.DefaultDelayMs);
        Assert.Equal(original.GlobalStopHotkey, clone.GlobalStopHotkey);
        Assert.Equal(original.MinimizeToTray, clone.MinimizeToTray);
        Assert.Equal(original.CloseToTray, clone.CloseToTray);
        Assert.Equal(original.StartWithWindows, clone.StartWithWindows);
        Assert.Equal(original.ShowNotifications, clone.ShowNotifications);
    }

    [Fact]
    public void AppSettings_CopyFrom_CopiesAllProperties()
    {
        var target = new AppSettings();
        var source = new AppSettings
        {
            Theme = AppTheme.Light,
            AutoTyperMasterEnabled = false,
            ConfirmOnDelete = false,
            DefaultDelayMs = 40,
            GlobalStopHotkey = "F12",
            MinimizeToTray = false,
            CloseToTray = false,
            StartWithWindows = true,
            ShowNotifications = false
        };

        target.CopyFrom(source);

        Assert.Equal(source.Theme, target.Theme);
        Assert.Equal(source.AutoTyperMasterEnabled, target.AutoTyperMasterEnabled);
        Assert.Equal(source.ConfirmOnDelete, target.ConfirmOnDelete);
        Assert.Equal(source.DefaultDelayMs, target.DefaultDelayMs);
        Assert.Equal(source.GlobalStopHotkey, target.GlobalStopHotkey);
        Assert.Equal(source.MinimizeToTray, target.MinimizeToTray);
        Assert.Equal(source.CloseToTray, target.CloseToTray);
        Assert.Equal(source.StartWithWindows, target.StartWithWindows);
        Assert.Equal(source.ShowNotifications, target.ShowNotifications);
    }

    [Fact]
    public void AppSettings_JsonSerialization_RoundTripsAccurately()
    {
        var original = new AppSettings
        {
            Theme = AppTheme.Dark,
            MinimizeToTray = true,
            CloseToTray = false,
            StartWithWindows = true,
            ShowNotifications = false
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Theme, restored.Theme);
        Assert.Equal(original.MinimizeToTray, restored.MinimizeToTray);
        Assert.Equal(original.CloseToTray, restored.CloseToTray);
        Assert.Equal(original.StartWithWindows, restored.StartWithWindows);
        Assert.Equal(original.ShowNotifications, restored.ShowNotifications);
    }

    [Fact]
    public void AppSettings_DeserializingLegacyJson_AppliesSafeDefaultsForNewFields()
    {
        // Legacy JSON without Phase 4 fields
        const string legacyJson = """
        {
            "Theme": 1,
            "AutoTyperMasterEnabled": true,
            "ConfirmOnDelete": true,
            "DefaultDelayMs": 20,
            "GlobalStopHotkey": "Escape"
        }
        """;

        var settings = JsonSerializer.Deserialize<AppSettings>(legacyJson);

        Assert.NotNull(settings);
        Assert.Equal(AppTheme.Light, settings.Theme);
        Assert.True(settings.MinimizeToTray);     // default
        Assert.True(settings.CloseToTray);        // default
        Assert.False(settings.StartWithWindows);  // default
        Assert.True(settings.ShowNotifications);  // default
    }

    [Fact]
    public void SettingsStorageService_CorruptedFile_RecoversWithDefaults()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"corrupted_settings_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, "{ this is invalid corrupted json content !!!");
            var service = new SettingsStorageService(tempFile);

            var settings = service.LoadSettings();

            Assert.NotNull(settings);
            Assert.True(settings.MinimizeToTray);
            Assert.True(settings.CloseToTray);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
