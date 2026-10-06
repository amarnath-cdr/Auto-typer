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

public class SearchTests
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
        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class MockHotkeyService : IHotkeyService
    {
        public event EventHandler<int>? HotkeyPressed;
        private readonly HashSet<int> _registered = new();

        public bool Register(int id, HotkeyModel hotkey)
        {
            _registered.Add(id);
            return true;
        }

        public bool Unregister(int id) => _registered.Remove(id);
        public void UnregisterAll() => _registered.Clear();
        public bool IsRegistered(int id) => _registered.Contains(id);
        public bool HasConflict(HotkeyModel hotkey, int? excludeId = null) => false;
        public void Dispose() => UnregisterAll();
        public void SimulateKeyPress(int id) => HotkeyPressed?.Invoke(this, id);
    }

    private static MainViewModel CreateViewModel(List<AutoTypeProfile> profiles)
    {
        var profileStorage = new MockProfileStorage { Profiles = profiles };
        var settingsStorage = new MockSettingsStorage();
        var themeService = new MockThemeService();
        var typingEngine = new TypingEngine(new MockKeyboardSimulator(), new MockDelayProvider());
        var hotkeyService = new MockHotkeyService();
        return new MainViewModel(profileStorage, settingsStorage, themeService, typingEngine, hotkeyService);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAllProfiles()
    {
        var profiles = new List<AutoTypeProfile>
        {
            new() { Name = "Profile A", Shortcut = "F1", Text = "Hello" },
            new() { Name = "Profile B", Shortcut = "F2", Text = "World" }
        };

        var vm = CreateViewModel(profiles);
        vm.SearchText = "";

        Assert.Equal(2, vm.Profiles.Count);
    }

    [Fact]
    public void Search_ByName_FiltersCaseInsensitively()
    {
        var profiles = new List<AutoTypeProfile>
        {
            new() { Name = "Welcome Message", Shortcut = "F1" },
            new() { Name = "Order Confirmation", Shortcut = "F2" },
            new() { Name = "General Notice", Shortcut = "F3" }
        };

        var vm = CreateViewModel(profiles);
        vm.SearchText = "order";

        Assert.Single(vm.Profiles);
        Assert.Equal("Order Confirmation", vm.Profiles[0].Name);
    }

    [Fact]
    public void Search_ByShortcut_Matches()
    {
        var profiles = new List<AutoTypeProfile>
        {
            new() { Name = "Profile 1", Shortcut = "F7" },
            new() { Name = "Profile 2", Shortcut = "Ctrl+Shift+A" }
        };

        var vm = CreateViewModel(profiles);
        vm.SearchText = "ctrl+shift";

        Assert.Single(vm.Profiles);
        Assert.Equal("Profile 2", vm.Profiles[0].Name);
    }

    [Fact]
    public void Search_ByComment_Matches()
    {
        var profiles = new List<AutoTypeProfile>
        {
            new() { Name = "Profile 1", Comment = "Urgent customer support reply" },
            new() { Name = "Profile 2", Comment = "Casual greeting" }
        };

        var vm = CreateViewModel(profiles);
        vm.SearchText = "support";

        Assert.Single(vm.Profiles);
        Assert.Equal("Profile 1", vm.Profiles[0].Name);
    }

    [Fact]
    public void Search_ByTextContent_Matches()
    {
        var profiles = new List<AutoTypeProfile>
        {
            new() { Name = "Profile 1", Text = "Lorem ipsum dolor sit amet" },
            new() { Name = "Profile 2", Text = "The quick brown fox" }
        };

        var vm = CreateViewModel(profiles);
        vm.SearchText = "quick brown";

        Assert.Single(vm.Profiles);
        Assert.Equal("Profile 2", vm.Profiles[0].Name);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var profiles = new List<AutoTypeProfile>
        {
            new() { Name = "Profile 1", Shortcut = "F1", Text = "ABC" }
        };

        var vm = CreateViewModel(profiles);
        vm.SearchText = "NonExistentXYZ";

        Assert.Empty(vm.Profiles);
    }
}
