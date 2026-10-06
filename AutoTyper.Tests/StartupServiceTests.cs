using System;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Services.Startup;
using AutoTyper.ViewModels;
using Xunit;

namespace AutoTyper.Tests;

public class StartupServiceTests
{
    private class MockStartupService : IStartupService
    {
        public bool IsEnabled { get; set; }
        public int SetCallCount { get; private set; }

        public bool IsStartWithWindowsEnabled() => IsEnabled;

        public bool SetStartWithWindows(bool enabled)
        {
            IsEnabled = enabled;
            SetCallCount++;
            return true;
        }
    }

    private class MockSettingsStorage : ISettingsStorageService
    {
        public AppSettings Settings { get; set; } = new();
        public AppSettings LoadSettings() => Settings;
        public void SaveSettings(AppSettings settings) => Settings = settings;
        public System.Threading.Tasks.Task<AppSettings> LoadSettingsAsync() => System.Threading.Tasks.Task.FromResult(Settings);
        public System.Threading.Tasks.Task SaveSettingsAsync(AppSettings settings)
        {
            Settings = settings;
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    private class MockThemeService : IThemeService
    {
        public AppTheme CurrentTheme { get; private set; }
        public void ApplyTheme(AppTheme theme) => CurrentTheme = theme;
    }

    [Fact]
    public void SettingsViewModel_InitializesStartupStateFromService()
    {
        var storage = new MockSettingsStorage();
        var theme = new MockThemeService();
        var startup = new MockStartupService { IsEnabled = true };

        var vm = new SettingsViewModel(storage, theme, startup);

        Assert.True(vm.StartWithWindows);
    }

    [Fact]
    public void SettingsViewModel_Save_UpdatesStartupServiceAndStorage()
    {
        var storage = new MockSettingsStorage();
        var theme = new MockThemeService();
        var startup = new MockStartupService { IsEnabled = false };

        var vm = new SettingsViewModel(storage, theme, startup)
        {
            StartWithWindows = true,
            MinimizeToTray = false,
            CloseToTray = false,
            ShowNotifications = false
        };

        vm.SaveCommand.Execute(null);

        Assert.True(startup.IsEnabled);
        Assert.Equal(1, startup.SetCallCount);
        Assert.True(storage.Settings.StartWithWindows);
        Assert.False(storage.Settings.MinimizeToTray);
        Assert.False(storage.Settings.CloseToTray);
        Assert.False(storage.Settings.ShowNotifications);
    }
}
