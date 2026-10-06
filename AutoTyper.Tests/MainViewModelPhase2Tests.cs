using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Services.Engine;
using AutoTyper.Services.Hotkeys;
using AutoTyper.Services.Keyboard;
using AutoTyper.ViewModels;
using Xunit;

namespace AutoTyper.Tests;

public class MainViewModelPhase2Tests
{
    private class MockThemeService : IThemeService
    {
        public AppTheme CurrentTheme => AppTheme.System;
        public void ApplyTheme(AppTheme theme) { }
    }

    private class MockProfileStorage : IProfileStorageService
    {
        public List<AutoTypeProfile> Profiles { get; set; } = new();
        public Task<List<AutoTypeProfile>> LoadProfilesAsync() => Task.FromResult(new List<AutoTypeProfile>(Profiles));
        public Task SaveProfilesAsync(IEnumerable<AutoTypeProfile> profiles)
        {
            Profiles = new List<AutoTypeProfile>(profiles);
            return Task.CompletedTask;
        }
        public List<AutoTypeProfile> LoadProfiles() => new(Profiles);
        public void SaveProfiles(IEnumerable<AutoTypeProfile> profiles) => Profiles = new List<AutoTypeProfile>(profiles);
        public List<AutoTypeProfile> GetDefaultProfiles() => new();
    }

    private class MockSettingsStorage : ISettingsStorageService
    {
        public AppSettings Settings { get; set; } = new();
        public Task<AppSettings> LoadSettingsAsync() => Task.FromResult(Settings);
        public Task SaveSettingsAsync(AppSettings settings)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
        public AppSettings LoadSettings() => Settings;
        public void SaveSettings(AppSettings settings) => Settings = settings;
    }

    private class MockKeyboardSimulator : IKeyboardSimulator
    {
        public List<char> TypedCharacters { get; } = new();
        public void SendCharacter(char character) => TypedCharacters.Add(character);
        public void SendKeyPress(ushort virtualKeyCode) { }
        public void SendKeyDown(ushort virtualKeyCode) { }
        public void SendKeyUp(ushort virtualKeyCode) { }
        public void SendKeyCombination(IReadOnlyList<ushort> modifiers, ushort targetKey) { }
        public void ReleaseAllModifiers() { }
    }

    private class MockDelayProvider : IDelayProvider
    {
        public Action? OnDelay { get; set; }
        public async Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            OnDelay?.Invoke();
            if (milliseconds > 0)
            {
                await Task.Delay(milliseconds, cancellationToken);
            }
        }
    }

    private class MockHotkeyService : IHotkeyService
    {
        public Dictionary<int, HotkeyModel> Registered { get; } = new();
        public event EventHandler<int>? HotkeyPressed;

        public bool Register(int id, HotkeyModel hotkey)
        {
            Registered[id] = hotkey;
            return true;
        }

        public bool Unregister(int id) => Registered.Remove(id);
        public void UnregisterAll() => Registered.Clear();
        public bool IsRegistered(int id) => Registered.ContainsKey(id);
        public bool HasConflict(HotkeyModel hotkey, int? excludeId = null) => false;
        public void Dispose() => UnregisterAll();
        public void TriggerHotkey(int id) => HotkeyPressed?.Invoke(this, id);
    }

    [Fact]
    public async Task StartTyping_ExecutesTypingAndTransitionsState()
    {
        var profile = new AutoTypeProfile { Name = "Test Profile", Text = "Hello", Shortcut = "F7" };
        var storage = new MockProfileStorage { Profiles = new List<AutoTypeProfile> { profile } };
        var settings = new MockSettingsStorage();
        var theme = new MockThemeService();
        var simulator = new MockKeyboardSimulator();
        var typingEngine = new TypingEngine(simulator, new MockDelayProvider());
        var hotkeyService = new MockHotkeyService();

        var vm = new MainViewModel(storage, settings, theme, typingEngine, hotkeyService);
        vm.SelectedProfile = profile;

        Assert.Equal(TypingState.Ready, vm.CurrentTypingState);
        Assert.False(vm.IsTyping);

        await vm.StartTypingAsync(profile);

        Assert.Equal(TypingState.Completed, vm.CurrentTypingState);
        Assert.Equal("Hello", new string(simulator.TypedCharacters.ToArray()));
    }

    [Fact]
    public async Task StopTyping_CancelsActiveTyping()
    {
        var profile = new AutoTypeProfile { Name = "Long Profile", Text = "Lots of characters to type here", Shortcut = "F7", TypingDelayMs = 10 };
        var storage = new MockProfileStorage { Profiles = new List<AutoTypeProfile> { profile } };
        var settings = new MockSettingsStorage();
        var theme = new MockThemeService();
        var simulator = new MockKeyboardSimulator();
        var delayProvider = new MockDelayProvider();
        var typingEngine = new TypingEngine(simulator, delayProvider);
        var hotkeyService = new MockHotkeyService();

        var vm = new MainViewModel(storage, settings, theme, typingEngine, hotkeyService);
        vm.SelectedProfile = profile;

        delayProvider.OnDelay = () => vm.StopTyping();

        // Start typing task
        await vm.StartTypingAsync(profile);

        // The state should end in Stopped
        Assert.Equal(TypingState.Stopped, vm.CurrentTypingState);
    }

    [Fact]
    public async Task HotkeyPress_TriggersTypingForMappedProfile()
    {
        var profile = new AutoTypeProfile { Name = "Shortcut Profile", Text = "From Hotkey", Shortcut = "F7", IsEnabled = true, TypingDelayMs = 0 };
        var storage = new MockProfileStorage { Profiles = new List<AutoTypeProfile> { profile } };
        var settings = new MockSettingsStorage();
        var theme = new MockThemeService();
        var simulator = new MockKeyboardSimulator();
        var typingEngine = new TypingEngine(simulator, new MockDelayProvider());
        var hotkeyService = new MockHotkeyService();

        var tcs = new TaskCompletionSource();
        typingEngine.StateChanged += (s, st) =>
        {
            if (st == TypingState.Completed)
                tcs.TrySetResult();
        };

        var vm = new MainViewModel(storage, settings, theme, typingEngine, hotkeyService);
        vm.RegisterGlobalHotkeys();

        // Verify that F7 was registered
        Assert.NotEmpty(hotkeyService.Registered);

        // Trigger the profile's hotkey
        int profileHotkeyId = -1;
        foreach (var kvp in hotkeyService.Registered)
        {
            if (kvp.Value.DisplayString == "F7")
            {
                profileHotkeyId = kvp.Key;
                break;
            }
        }

        Assert.True(profileHotkeyId > 0);
        hotkeyService.TriggerHotkey(profileHotkeyId);

        // Allow async typing to complete deterministically
        await tcs.Task;

        Assert.Equal("From Hotkey", new string(simulator.TypedCharacters.ToArray()));
    }

    [Fact]
    public void MasterEnabled_Disabled_UnregistersAllHotkeys()
    {
        var profile = new AutoTypeProfile { Name = "Profile", Shortcut = "F7", IsEnabled = true };
        var storage = new MockProfileStorage { Profiles = new List<AutoTypeProfile> { profile } };
        var settings = new MockSettingsStorage();
        var theme = new MockThemeService();
        var simulator = new MockKeyboardSimulator();
        var typingEngine = new TypingEngine(simulator, new MockDelayProvider());
        var hotkeyService = new MockHotkeyService();

        var vm = new MainViewModel(storage, settings, theme, typingEngine, hotkeyService);
        vm.RegisterGlobalHotkeys();
        Assert.NotEmpty(hotkeyService.Registered);

        vm.IsMasterEnabled = false;
        Assert.Empty(hotkeyService.Registered);

        vm.IsMasterEnabled = true;
        Assert.NotEmpty(hotkeyService.Registered);
    }
}
