using System.Collections.Generic;
using System.IO;
using AutoTyper.Models;
using AutoTyper.Services;
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

    private static MainViewModel CreateViewModel(List<AutoTypeProfile> profiles)
    {
        var profileStorage = new MockProfileStorage { Profiles = profiles };
        var settingsStorage = new MockSettingsStorage();
        var themeService = new MockThemeService();
        return new MainViewModel(profileStorage, settingsStorage, themeService);
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
