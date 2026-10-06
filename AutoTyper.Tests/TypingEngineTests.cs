using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoTyper.Models;
using AutoTyper.Services.Engine;
using AutoTyper.Services.Keyboard;
using Xunit;

namespace AutoTyper.Tests;

public class TypingEngineTests
{
    private class TestKeyboardSimulator : IKeyboardSimulator
    {
        public List<char> TypedCharacters { get; } = new();
        public List<ushort> KeyPresses { get; } = new();
        public int ReleaseModifiersCallCount { get; private set; }
        public bool ShouldThrowOnChar { get; set; }
        public char ThrowOnChar { get; set; }

        public void SendCharacter(char character)
        {
            if (ShouldThrowOnChar && character == ThrowOnChar)
            {
                throw new InvalidOperationException($"Simulated failure on character '{character}'");
            }
            TypedCharacters.Add(character);
        }

        public void SendKeyPress(ushort virtualKeyCode)
        {
            KeyPresses.Add(virtualKeyCode);
        }

        public void SendKeyDown(ushort virtualKeyCode)
        {
        }

        public void SendKeyUp(ushort virtualKeyCode)
        {
        }

        public void SendKeyCombination(IReadOnlyList<ushort> modifiers, ushort targetKey)
        {
        }

        public void ReleaseAllModifiers()
        {
            ReleaseModifiersCallCount++;
        }
    }

    private class TestDelayProvider : IDelayProvider
    {
        public List<int> RecordedDelays { get; } = new();
        public Action<int>? OnDelay { get; set; }

        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            RecordedDelays.Add(milliseconds);
            OnDelay?.Invoke(milliseconds);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task TypeTextAsync_TypesEveryCharacterInOrder()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);
        const string input = "Hello, AutoTyper 2.0!";

        // Act
        await engine.TypeTextAsync(input, 10, false, 5, 20, CancellationToken.None);

        // Assert
        Assert.Equal(input, new string(simulator.TypedCharacters.ToArray()));
        Assert.Equal(TypingState.Completed, engine.CurrentState);
    }

    [Fact]
    public async Task TypeTextAsync_EmptyText_CompletesImmediately()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);

        // Act
        await engine.TypeTextAsync("", 20, false, 10, 30, CancellationToken.None);

        // Assert
        Assert.Empty(simulator.TypedCharacters);
        Assert.Empty(delayProvider.RecordedDelays);
        Assert.Equal(TypingState.Completed, engine.CurrentState);
    }

    [Fact]
    public async Task TypeTextAsync_AppliesConstantDelayBetweenCharacters()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);
        const string input = "ABCD"; // 4 chars -> 3 delay intervals
        const int expectedDelay = 45;

        // Act
        await engine.TypeTextAsync(input, expectedDelay, false, 10, 50, CancellationToken.None);

        // Assert
        Assert.Equal(3, delayProvider.RecordedDelays.Count);
        Assert.All(delayProvider.RecordedDelays, d => Assert.Equal(expectedDelay, d));
    }

    [Fact]
    public async Task TypeTextAsync_WithJitter_DelaysAreWithinBounds()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);
        const string input = "RandomJitterDelayTestStringWithLotsOfChars";
        const int minDelay = 15;
        const int maxDelay = 35;

        // Act
        await engine.TypeTextAsync(input, 20, true, minDelay, maxDelay, CancellationToken.None);

        // Assert
        Assert.NotEmpty(delayProvider.RecordedDelays);
        Assert.All(delayProvider.RecordedDelays, d =>
        {
            Assert.InRange(d, minDelay, maxDelay);
        });
    }

    [Fact]
    public async Task TypeTextAsync_CancellationDuringTyping_StopsAndReleasesModifiers()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);
        using var cts = new CancellationTokenSource();

        delayProvider.OnDelay = count =>
        {
            if (simulator.TypedCharacters.Count >= 3)
            {
                cts.Cancel();
            }
        };

        const string input = "LongStringToCancelMidwayThrough";

        // Act
        await engine.TypeTextAsync(input, 10, false, 5, 20, cts.Token);

        // Assert
        Assert.Equal(TypingState.Stopped, engine.CurrentState);
        Assert.True(simulator.TypedCharacters.Count < input.Length);
        Assert.True(simulator.ReleaseModifiersCallCount > 0);
    }

    [Fact]
    public async Task TypeTextAsync_AlreadyCancelledToken_StopsImmediately()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await engine.TypeTextAsync("ImmediateCancel", 20, false, 10, 30, cts.Token);

        // Assert
        Assert.Equal(TypingState.Stopped, engine.CurrentState);
        Assert.Empty(simulator.TypedCharacters);
        Assert.True(simulator.ReleaseModifiersCallCount > 0);
    }

    [Fact]
    public async Task TypeTextAsync_OnException_TransitionsToErrorAndReleasesModifiers()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator
        {
            ShouldThrowOnChar = true,
            ThrowOnChar = 'X'
        };
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await engine.TypeTextAsync("ABCXDEF", 10, false, 5, 20, CancellationToken.None);
        });

        Assert.Equal(TypingState.Error, engine.CurrentState);
        Assert.True(simulator.ReleaseModifiersCallCount > 0);
    }

    [Fact]
    public async Task TypeTextAsync_ReportsProgressAccurately()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);
        var progressList = new List<TypingProgress>();

        engine.ProgressChanged += (s, p) => progressList.Add(new TypingProgress
        {
            CurrentIndex = p.CurrentIndex,
            TotalCharacters = p.TotalCharacters
        });

        const string input = "12345";

        // Act
        await engine.TypeTextAsync(input, 10, false, 5, 20, CancellationToken.None);

        // Assert
        Assert.Equal(5, progressList.Count);
        for (int i = 0; i < progressList.Count; i++)
        {
            Assert.Equal(i + 1, progressList[i].CurrentIndex);
            Assert.Equal(5, progressList[i].TotalCharacters);
        }
    }

    [Fact]
    public async Task TypeTextAsync_HandlesVeryLongText()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var engine = new TypingEngine(simulator, delayProvider);
        var longText = new string('A', 1000);

        // Act
        await engine.TypeTextAsync(longText, 0, false, 0, 0, CancellationToken.None);

        // Assert
        Assert.Equal(1000, simulator.TypedCharacters.Count);
        Assert.Equal(TypingState.Completed, engine.CurrentState);
    }

    [Fact]
    public async Task TypeProfileAsync_WithWaitToken_PausesExplicitly()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "A{WAIT:500}B",
            TypingDelayMs = 20,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        // Act
        await engine.TypeProfileAsync(profile, CancellationToken.None);

        // Assert
        Assert.Equal(2, simulator.TypedCharacters.Count); // A and B
        
        // Expected delays:
        // After 'A', standard delay 20ms
        // Then WAIT token delays 500ms
        // So RecordedDelays should contain 20, then 500.
        // B is last, no delay after B.
        Assert.Equal(2, delayProvider.RecordedDelays.Count);
        Assert.Equal(20, delayProvider.RecordedDelays[0]);
        Assert.Equal(500, delayProvider.RecordedDelays[1]);
    }

    [Fact]
    public async Task TypeProfileAsync_WithMultipleWaitTokens_PausesExplicitly()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "{WAIT:100}AB{WAIT:200}",
            TypingDelayMs = 10,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        // Act
        await engine.TypeProfileAsync(profile, CancellationToken.None);

        // Assert
        // Delays: WAIT 100 -> A -> delay 10 -> B -> delay 10 -> WAIT 200
        Assert.Equal(4, delayProvider.RecordedDelays.Count);
        Assert.Equal(100, delayProvider.RecordedDelays[0]);
        Assert.Equal(10, delayProvider.RecordedDelays[1]);
        Assert.Equal(10, delayProvider.RecordedDelays[2]);
        Assert.Equal(200, delayProvider.RecordedDelays[3]);
    }
}
