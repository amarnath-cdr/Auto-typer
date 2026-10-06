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

public class DuplicateProfileTests
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
        public void SendCharacter(char character) { }
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
        public event EventHandler<int>? HotkeyPressed { add { } remove { } }
        public bool Register(int id, HotkeyModel hotkey) => true;
        public bool Unregister(int id) => true;
        public void UnregisterAll() { }
        public bool IsRegistered(int id) => false;
        public bool HasConflict(HotkeyModel hotkey, int? excludeId = null) => false;
        public void Dispose() { }
    }

    [Fact]
    public void Clone_GeneratesNewIdAndAppendsCopySuffix()
    {
        var original = new AutoTypeProfile
        {
            Id = Guid.NewGuid(),
            Name = "Email Signature",
            Shortcut = "F8",
            Text = "Kind regards",
            Comment = "Company signature",
            TypingDelayMs = 35,
            UseJitter = true,
            MinDelayMs = 15,
            MaxDelayMs = 60,
            TypingMode = TypingMode.Clipboard,
            Capitalization = CapitalizationMode.Original,
            RepeatCount = 2,
            IsEnabled = true
        };

        var clone = original.Clone();

        Assert.NotEqual(original.Id, clone.Id);
        Assert.Equal("Email Signature (Copy)", clone.Name);
        Assert.Equal(original.Shortcut, clone.Shortcut);
        Assert.Equal(original.Text, clone.Text);
        Assert.Equal(original.Comment, clone.Comment);
        Assert.Equal(original.TypingDelayMs, clone.TypingDelayMs);
        Assert.True(clone.UseJitter);
        Assert.Equal(15, clone.MinDelayMs);
        Assert.Equal(60, clone.MaxDelayMs);
        Assert.Equal(original.TypingMode, clone.TypingMode);
        Assert.Equal(original.Capitalization, clone.Capitalization);
        Assert.Equal(original.RepeatCount, clone.RepeatCount);
        Assert.Equal(original.IsEnabled, clone.IsEnabled);
    }

    [Fact]
    public void ViewModel_DuplicateCommand_AppendsClonedProfileAndSelectsIt()
    {
        var original = new AutoTypeProfile
        {
            Name = "Greeting",
            Shortcut = "F7",
            Text = "Hello"
        };

        var storage = new MockProfileStorage { Profiles = new List<AutoTypeProfile> { original } };
        var settings = new MockSettingsStorage();
        var theme = new MockThemeService();
        var typingEngine = new TypingEngine(new MockKeyboardSimulator(), new MockDelayProvider());
        var hotkeyService = new MockHotkeyService();
        var vm = new MainViewModel(storage, settings, theme, typingEngine, hotkeyService);

        vm.SelectedProfile = vm.Profiles[0];
        Assert.True(vm.DuplicateCommand.CanExecute(null));

        vm.DuplicateCommand.Execute(null);

        Assert.Equal(2, vm.Profiles.Count);
        Assert.NotNull(vm.SelectedProfile);
        Assert.Equal("Greeting (Copy)", vm.SelectedProfile.Name);
        Assert.Equal(2, storage.Profiles.Count);
    }
}
