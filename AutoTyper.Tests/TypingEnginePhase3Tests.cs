using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoTyper.Models;
using AutoTyper.Services.Clipboard;
using AutoTyper.Services.Engine;
using AutoTyper.Services.Keyboard;
using Xunit;

namespace AutoTyper.Tests;

public class TypingEnginePhase3Tests
{
    private class MockKeyboardSimulator : IKeyboardSimulator
    {
        public List<char> Characters { get; } = new();
        public List<ushort> KeyPresses { get; } = new();
        public List<ushort> KeyDowns { get; } = new();
        public List<ushort> KeyUps { get; } = new();
        public List<(IReadOnlyList<ushort> Modifiers, ushort Target)> KeyCombinations { get; } = new();
        public int ReleaseModifiersCount { get; private set; }

        public void SendCharacter(char character) => Characters.Add(character);
        public void SendKeyPress(ushort virtualKeyCode) => KeyPresses.Add(virtualKeyCode);
        public void SendKeyDown(ushort virtualKeyCode) => KeyDowns.Add(virtualKeyCode);
        public void SendKeyUp(ushort virtualKeyCode) => KeyUps.Add(virtualKeyCode);
        public void SendKeyCombination(IReadOnlyList<ushort> modifiers, ushort targetKey)
        {
            KeyCombinations.Add((modifiers, targetKey));
        }
        public void ReleaseAllModifiers() => ReleaseModifiersCount++;
    }

    private class MockDelayProvider : IDelayProvider
    {
        public List<int> Delays { get; } = new();
        public Action<int>? OnDelay { get; set; }

        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            Delays.Add(milliseconds);
            OnDelay?.Invoke(milliseconds);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private class MockClipboardService : IClipboardService
    {
        public string? Content { get; set; }
        public List<string> SetHistory { get; } = new();
        public int ClearCount { get; set; }

        public string? GetText() => Content;

        public void SetText(string text)
        {
            Content = text;
            SetHistory.Add(text);
        }

        public void Clear()
        {
            Content = null;
            ClearCount++;
        }
    }

    [Fact]
    public async Task TypeProfileAsync_WithStartDelay_WaitsBeforeTyping()
    {
        var simulator = new MockKeyboardSimulator();
        var delayProvider = new MockDelayProvider();
        var parser = new TypingParser();
        var clipboard = new MockClipboardService();
        var engine = new TypingEngine(simulator, delayProvider, parser, clipboard);

        var profile = new AutoTypeProfile
        {
            Name = "Test",
            Text = "Hi",
            StartDelayMs = 500,
            TypingDelayMs = 10,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);

        Assert.Equal(TypingState.Completed, engine.CurrentState);
        Assert.NotEmpty(delayProvider.Delays);
        Assert.Equal(500, delayProvider.Delays[0]);
        Assert.Equal("Hi", new string(simulator.Characters.ToArray()));
    }

    [Fact]
    public async Task TypeProfileAsync_CancelledDuringStartDelay_CancelsCleanlyAndReleasesModifiers()
    {
        var simulator = new MockKeyboardSimulator();
        var delayProvider = new MockDelayProvider();
        var parser = new TypingParser();
        var clipboard = new MockClipboardService();
        var engine = new TypingEngine(simulator, delayProvider, parser, clipboard);

        using var cts = new CancellationTokenSource();
        delayProvider.OnDelay = d =>
        {
            if (d == 1000)
            {
                cts.Cancel();
            }
        };

        var profile = new AutoTypeProfile
        {
            Text = "Never typed",
            StartDelayMs = 1000,
            TypingMode = TypingMode.Simulated
        };

        await engine.TypeProfileAsync(profile, cts.Token);

        Assert.Equal(TypingState.Stopped, engine.CurrentState);
        Assert.Empty(simulator.Characters);
        Assert.True(simulator.ReleaseModifiersCount > 0);
    }

    [Fact]
    public async Task TypeProfileAsync_WithRepeatCount_RepeatsExecution()
    {
        var simulator = new MockKeyboardSimulator();
        var delayProvider = new MockDelayProvider();
        var parser = new TypingParser();
        var clipboard = new MockClipboardService();
        var engine = new TypingEngine(simulator, delayProvider, parser, clipboard);

        var profile = new AutoTypeProfile
        {
            Text = "A",
            RepeatCount = 3,
            TypingDelayMs = 10,
            TypingMode = TypingMode.Simulated
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);

        Assert.Equal(TypingState.Completed, engine.CurrentState);
        Assert.Equal(3, simulator.Characters.Count);
        Assert.Equal("AAA", new string(simulator.Characters.ToArray()));
    }

    [Fact]
    public async Task TypeProfileAsync_ExecutesSpecialKeys()
    {
        var simulator = new MockKeyboardSimulator();
        var delayProvider = new MockDelayProvider();
        var parser = new TypingParser();
        var clipboard = new MockClipboardService();
        var engine = new TypingEngine(simulator, delayProvider, parser, clipboard);

        var profile = new AutoTypeProfile
        {
            Text = "A{ENTER}B{TAB}",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);

        Assert.Equal(TypingState.Completed, engine.CurrentState);
        Assert.Equal(new char[] { 'A', 'B' }, simulator.Characters);
        Assert.Equal(new ushort[] { 0x0D, 0x09 }, simulator.KeyPresses);
    }

    [Fact]
    public async Task TypeProfileAsync_ExecutesKeyCombinations()
    {
        var simulator = new MockKeyboardSimulator();
        var delayProvider = new MockDelayProvider();
        var parser = new TypingParser();
        var clipboard = new MockClipboardService();
        var engine = new TypingEngine(simulator, delayProvider, parser, clipboard);

        var profile = new AutoTypeProfile
        {
            Text = "{CTRL+C}{ALT+TAB}",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);

        Assert.Equal(TypingState.Completed, engine.CurrentState);
        Assert.Equal(2, simulator.KeyCombinations.Count);
        Assert.Equal(0x43, simulator.KeyCombinations[0].Target); // 'C'
        Assert.Equal(0x09, simulator.KeyCombinations[1].Target); // TAB
    }

    [Theory]
    [InlineData(CapitalizationMode.Original, "Hello World", "Hello World")]
    [InlineData(CapitalizationMode.Uppercase, "Hello World", "HELLO WORLD")]
    [InlineData(CapitalizationMode.Lowercase, "Hello World", "hello world")]
    [InlineData(CapitalizationMode.SentenceCase, "hello world. this is test.", "Hello world. This is test.")]
    public async Task TypeProfileAsync_AppliesCapitalizationCorrectly(CapitalizationMode mode, string input, string expected)
    {
        var simulator = new MockKeyboardSimulator();
        var delayProvider = new MockDelayProvider();
        var parser = new TypingParser();
        var clipboard = new MockClipboardService();
        var engine = new TypingEngine(simulator, delayProvider, parser, clipboard);

        var profile = new AutoTypeProfile
        {
            Text = input,
            Capitalization = mode,
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);

        Assert.Equal(expected, new string(simulator.Characters.ToArray()));
    }

    [Fact]
    public async Task TypeProfileAsync_ClipboardMode_PastesAndRestoresClipboard()
    {
        var simulator = new MockKeyboardSimulator();
        var delayProvider = new MockDelayProvider();
        var parser = new TypingParser();
        var clipboard = new MockClipboardService { Content = "Original Clipboard Content" };
        var engine = new TypingEngine(simulator, delayProvider, parser, clipboard);

        var profile = new AutoTypeProfile
        {
            Text = "Pasted Profile Text",
            TypingMode = TypingMode.Clipboard,
            RepeatCount = 1
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);

        Assert.Equal(TypingState.Completed, engine.CurrentState);
        // Verified that Ctrl+V was simulated (VK_CONTROL = 0x11, 'V' = 0x56)
        Assert.Single(simulator.KeyCombinations);
        Assert.Equal(0x56, simulator.KeyCombinations[0].Target);
        // Verified that original clipboard content was restored
        Assert.Equal("Original Clipboard Content", clipboard.Content);
        // Verified that profile text was placed onto clipboard temporarily
        Assert.Contains("Pasted Profile Text", clipboard.SetHistory);
    }
}
