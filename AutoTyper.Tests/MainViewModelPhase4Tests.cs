using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Services.Engine;
using AutoTyper.Services.Hotkeys;
using AutoTyper.Services.Keyboard;
using AutoTyper.Services.Notifications;
using AutoTyper.Services.Tray;
using AutoTyper.ViewModels;
using Xunit;

namespace AutoTyper.Tests;

public class MainViewModelPhase4Tests
{
    private class MockProfileStorage : IProfileStorageService
    {
        public List<AutoTypeProfile> Profiles { get; set; } = new();
        public List<AutoTypeProfile> LoadProfiles() => new(Profiles);
        public void SaveProfiles(IEnumerable<AutoTypeProfile> profiles) => Profiles = new(profiles);
        public List<AutoTypeProfile> GetDefaultProfiles() => new();
        public Task<List<AutoTypeProfile>> LoadProfilesAsync() => Task.FromResult(new List<AutoTypeProfile>(Profiles));
        public Task SaveProfilesAsync(IEnumerable<AutoTypeProfile> profiles)
        {
            Profiles = new List<AutoTypeProfile>(profiles);
            return Task.CompletedTask;
        }
    }

    private class MockSettingsStorage : ISettingsStorageService
    {
        public AppSettings Settings { get; set; } = new();
        public AppSettings LoadSettings() => Settings;
        public void SaveSettings(AppSettings settings) => Settings = settings;
        public Task<AppSettings> LoadSettingsAsync() => Task.FromResult(Settings);
        public Task SaveSettingsAsync(AppSettings settings)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
    }

    private class MockThemeService : IThemeService
    {
        public AppTheme CurrentTheme { get; private set; }
        public void ApplyTheme(AppTheme theme) => CurrentTheme = theme;
    }

    private class MockKeyboardSimulator : IKeyboardSimulator
    {
        public List<char> Typed { get; } = new();
        public void SendCharacter(char character) => Typed.Add(character);
        public void SendKeyPress(ushort virtualKeyCode) { }
        public void SendKeyDown(ushort virtualKeyCode) { }
        public void SendKeyUp(ushort virtualKeyCode) { }
        public void SendKeyCombination(IReadOnlyList<ushort> modifiers, ushort targetKey) { }
        public void ReleaseAllModifiers() { }
    }

    private class MockDelayProvider : IDelayProvider
    {
        public Action? OnDelay { get; set; }
        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            OnDelay?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private class MockHotkeyService : IHotkeyService
    {
        public event EventHandler<int>? HotkeyPressed { add { } remove { } }
        public bool Register(int id, HotkeyModel hotkey) => true;
        public bool Unregister(int id) => true;
        public void UnregisterAll() { }
        public bool IsRegistered(int id) => true;
        public bool HasConflict(HotkeyModel hotkey, int? excludeId = null) => false;
        public void Dispose() { }
    }

    private class MockNotificationService : INotificationService
    {
        public List<(string Title, string Message, NotificationType Type)> Notifications { get; } = new();
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Information)
        {
            Notifications.Add((title, message, type));
        }
    }

    private class MockTrayIconService : ITrayIconService
    {
        public bool CanStartSelected { get; private set; }
        public bool IsTyping { get; private set; }
        public int UpdateCount { get; private set; }

        public void Initialize(Action onShow, Action onStartSelected, Action onStop, Action onExit) { }
        public void UpdateMenuState(bool canStartSelected, bool isTyping)
        {
            CanStartSelected = canStartSelected;
            IsTyping = isTyping;
            UpdateCount++;
        }
        public void ShowBalloonTip(string title, string message, NotificationType type, int timeoutMs = 3000) { }
        public void SetVisible(bool visible) { }
        public void Dispose() { }
    }

    [Fact]
    public async Task StartTypingAsync_EmitsStartAndCompletionNotifications()
    {
        var profile = new AutoTypeProfile { Name = "Welcome Message", Text = "Hi", Shortcut = "F7" };
        var storage = new MockProfileStorage { Profiles = new List<AutoTypeProfile> { profile } };
        var settings = new MockSettingsStorage();
        var theme = new MockThemeService();
        var simulator = new MockKeyboardSimulator();
        var engine = new TypingEngine(simulator, new MockDelayProvider());
        var hotkeys = new MockHotkeyService();
        var notifications = new MockNotificationService();
        var tray = new MockTrayIconService();

        var vm = new MainViewModel(storage, settings, theme, engine, hotkeys, notifications, tray);
        vm.SelectedProfile = profile;

        await vm.StartTypingAsync(profile);

        Assert.Equal(2, notifications.Notifications.Count);
        Assert.Contains(notifications.Notifications, n => n.Message.Contains("Typing started: Welcome Message"));
        Assert.Contains(notifications.Notifications, n => n.Message.Contains("Typing completed: Welcome Message"));
        Assert.Equal(TypingState.Completed, vm.CurrentTypingState);
    }

    [Fact]
    public async Task StopTyping_CancelsTypingAndEmitsStoppedNotification()
    {
        var profile = new AutoTypeProfile { Name = "Long Task", Text = "Very long text to cancel", Shortcut = "F7", TypingDelayMs = 10 };
        var storage = new MockProfileStorage { Profiles = new List<AutoTypeProfile> { profile } };
        var settings = new MockSettingsStorage();
        var theme = new MockThemeService();
        var simulator = new MockKeyboardSimulator();
        var delay = new MockDelayProvider();
        var engine = new TypingEngine(simulator, delay);
        var hotkeys = new MockHotkeyService();
        var notifications = new MockNotificationService();
        var tray = new MockTrayIconService();

        var vm = new MainViewModel(storage, settings, theme, engine, hotkeys, notifications, tray);
        vm.SelectedProfile = profile;

        delay.OnDelay = () => vm.StopTyping();

        await vm.StartTypingAsync(profile);

        Assert.Equal(TypingState.Stopped, vm.CurrentTypingState);
        Assert.Contains(notifications.Notifications, n => n.Message.Contains("Typing stopped"));
    }

    [Fact]
    public void SelectionAndStateChanges_UpdateTrayIconMenu()
    {
        var profile = new AutoTypeProfile { Name = "Profile A", Text = "Test", Shortcut = "F7", IsEnabled = true };
        var storage = new MockProfileStorage { Profiles = new List<AutoTypeProfile> { profile } };
        var settings = new MockSettingsStorage();
        var theme = new MockThemeService();
        var simulator = new MockKeyboardSimulator();
        var engine = new TypingEngine(simulator, new MockDelayProvider());
        var hotkeys = new MockHotkeyService();
        var notifications = new MockNotificationService();
        var tray = new MockTrayIconService();

        var vm = new MainViewModel(storage, settings, theme, engine, hotkeys, notifications, tray);

        Assert.True(tray.UpdateCount > 0);
        Assert.False(tray.CanStartSelected); // initially null selected

        vm.SelectedProfile = profile;
        Assert.True(tray.CanStartSelected);
        Assert.False(tray.IsTyping);

        vm.SelectedProfile = null;
        Assert.False(tray.CanStartSelected);
    }
}
