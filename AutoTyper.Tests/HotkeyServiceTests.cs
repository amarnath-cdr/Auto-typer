using System;
using System.Collections.Generic;
using AutoTyper.Services.Hotkeys;
using Xunit;

namespace AutoTyper.Tests;

public class HotkeyServiceTests
{
    private class TestHotkeyService : IHotkeyService
    {
        private readonly Dictionary<int, HotkeyModel> _registrations = new();
        public event EventHandler<int>? HotkeyPressed;

        public bool Register(int id, HotkeyModel hotkey)
        {
            if (HasConflict(hotkey, id))
                return false;

            _registrations[id] = hotkey;
            return true;
        }

        public bool Unregister(int id) => _registrations.Remove(id);

        public void UnregisterAll() => _registrations.Clear();

        public bool IsRegistered(int id) => _registrations.ContainsKey(id);

        public bool HasConflict(HotkeyModel hotkey, int? excludeId = null)
        {
            var key = hotkey.GetUniqueKey();
            foreach (var kvp in _registrations)
            {
                if (excludeId.HasValue && kvp.Key == excludeId.Value)
                    continue;

                if (kvp.Value.GetUniqueKey() == key)
                    return true;
            }
            return false;
        }

        public void FireHotkey(int id) => HotkeyPressed?.Invoke(this, id);

        public void Dispose() => UnregisterAll();
    }

    [Theory]
    [InlineData("F7", 0x76, (uint)0x4000)]
    [InlineData("Ctrl+F8", 0x77, (uint)(0x4000 | 0x0002))]
    [InlineData("Alt+Shift+T", 0x54, (uint)(0x4000 | 0x0001 | 0x0004))]
    [InlineData("Ctrl+Alt+Shift+Win+A", 0x41, (uint)(0x4000 | 0x0002 | 0x0001 | 0x0004 | 0x0008))]
    [InlineData("Escape", 0x1B, (uint)0x4000)]
    [InlineData("Tab", 0x09, (uint)0x4000)]
    [InlineData("Space", 0x20, (uint)0x4000)]
    public void HotkeyModel_TryParse_ValidHotkeys_ParsesCorrectly(string input, uint expectedVk, uint expectedModifiers)
    {
        bool success = HotkeyModel.TryParse(input, out var hotkey);

        Assert.True(success);
        Assert.NotNull(hotkey);
        Assert.Equal(expectedVk, hotkey.VirtualKeyCode);
        Assert.Equal(expectedModifiers, hotkey.Modifiers);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("InvalidKeyName")]
    [InlineData("Ctrl+")]
    [InlineData("UnknownModifier+F1")]
    public void HotkeyModel_TryParse_InvalidHotkeys_ReturnsFalse(string? input)
    {
        bool success = HotkeyModel.TryParse(input, out var hotkey);

        Assert.False(success);
        Assert.Null(hotkey);
    }

    [Fact]
    public void HotkeyModel_EqualityAndUniqueKey_MatchesSameCombination()
    {
        HotkeyModel.TryParse("Ctrl+F7", out var hotkey1);
        HotkeyModel.TryParse("ctrl+f7", out var hotkey2);
        HotkeyModel.TryParse("Alt+F7", out var hotkey3);

        Assert.NotNull(hotkey1);
        Assert.NotNull(hotkey2);
        Assert.NotNull(hotkey3);

        Assert.Equal(hotkey1, hotkey2);
        Assert.NotEqual(hotkey1, hotkey3);
        Assert.Equal(hotkey1.GetUniqueKey(), hotkey2.GetUniqueKey());
        Assert.NotEqual(hotkey1.GetUniqueKey(), hotkey3.GetUniqueKey());
    }

    [Fact]
    public void HotkeyService_RegisterAndUnregister_TracksState()
    {
        var service = new TestHotkeyService();
        HotkeyModel.TryParse("F7", out var hotkey);
        Assert.NotNull(hotkey);

        bool registered = service.Register(1, hotkey);
        Assert.True(registered);
        Assert.True(service.IsRegistered(1));

        bool unregistered = service.Unregister(1);
        Assert.True(unregistered);
        Assert.False(service.IsRegistered(1));
    }

    [Fact]
    public void HotkeyService_ConflictDetection_DetectsDuplicateKeys()
    {
        var service = new TestHotkeyService();
        HotkeyModel.TryParse("Ctrl+Shift+A", out var hotkey1);
        HotkeyModel.TryParse("CTRL+SHIFT+A", out var hotkey2);
        HotkeyModel.TryParse("Ctrl+Shift+B", out var hotkey3);

        Assert.NotNull(hotkey1);
        Assert.NotNull(hotkey2);
        Assert.NotNull(hotkey3);

        service.Register(1, hotkey1);

        Assert.True(service.HasConflict(hotkey2));
        Assert.False(service.HasConflict(hotkey2, excludeId: 1)); // Self-exclusion
        Assert.False(service.HasConflict(hotkey3));
    }

    [Fact]
    public void HotkeyService_UnregisterAll_ClearsRegistrations()
    {
        var service = new TestHotkeyService();
        HotkeyModel.TryParse("F1", out var h1);
        HotkeyModel.TryParse("F2", out var h2);

        service.Register(1, h1!);
        service.Register(2, h2!);

        service.UnregisterAll();

        Assert.False(service.IsRegistered(1));
        Assert.False(service.IsRegistered(2));
    }

    [Fact]
    public void HotkeyService_EventTriggering_NotifiesListeners()
    {
        var service = new TestHotkeyService();
        int? receivedId = null;
        service.HotkeyPressed += (s, id) => receivedId = id;

        service.FireHotkey(42);

        Assert.Equal(42, receivedId);
    }
}
