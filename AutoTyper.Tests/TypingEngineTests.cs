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

    [Fact]
    public async Task TypeProfileAsync_WithWaitRangeToken_DelaysWithinRange()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "A{WAIT:20-400}B",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        // Act
        await engine.TypeProfileAsync(profile, CancellationToken.None);

        // Assert
        Assert.Equal(2, simulator.TypedCharacters.Count);
        Assert.Single(delayProvider.RecordedDelays);
        int waitDelay = delayProvider.RecordedDelays[0];
        Assert.InRange(waitDelay, 20, 400);
    }

    [Fact]
    public async Task TypeProfileAsync_WithWaitRangeAndRepetition_EvaluatesIndependentlyEachTime()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "X{WAIT:10-1000}",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 10
        };

        // Act
        await engine.TypeProfileAsync(profile, CancellationToken.None);

        // Assert
        Assert.Equal(10, simulator.TypedCharacters.Count);
        Assert.Equal(10, delayProvider.RecordedDelays.Count);

        // Every delay must be within [10, 1000]
        foreach (var delay in delayProvider.RecordedDelays)
        {
            Assert.InRange(delay, 10, 1000);
        }

        // Independent evaluation check: With range [10, 1000] and 10 repetitions,
        // it is statistically impossible for all 10 independent random numbers to be identical.
        var distinctDelays = delayProvider.RecordedDelays.Distinct().Count();
        Assert.True(distinctDelays > 1, "Delays should not all be identical across 10 repetitions.");
    }

    [Fact]
    public async Task TypeProfileAsync_CancellationDuringWaitRange_StopsPromptly()
    {
        // Arrange
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);
        using var cts = new CancellationTokenSource();

        delayProvider.OnDelay = (ms) =>
        {
            if (ms >= 100)
            {
                cts.Cancel();
            }
        };

        var profile = new AutoTypeProfile
        {
            Text = "A{WAIT:200-500}B",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        // Act
        await engine.TypeProfileAsync(profile, cts.Token);

        // Assert
        Assert.Equal(TypingState.Stopped, engine.CurrentState);
        Assert.Contains('A', simulator.TypedCharacters);
        Assert.DoesNotContain('B', simulator.TypedCharacters);
    }

    [Fact]
    public async Task ManualScenario1_WaitVariesAcrossMultipleRuns()
    {
        // 1. WH{WAIT:20-400}WB — run multiple times and verify the pause varies
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "WH{WAIT:20-400}WB",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        var delays = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            delayProvider.RecordedDelays.Clear();
            await engine.TypeProfileAsync(profile, CancellationToken.None);
            Assert.Single(delayProvider.RecordedDelays);
            int d = delayProvider.RecordedDelays[0];
            Assert.InRange(d, 20, 400);
            delays.Add(d);
        }

        Assert.True(delays.Distinct().Count() > 1, $"Delays across multiple runs should vary: {string.Join(", ", delays)}");
    }

    [Fact]
    public async Task ManualScenario2_WaitStaysWithinRange()
    {
        // 2. A{WAIT:100-500}B — verify the pause stays within the range
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "A{WAIT:100-500}B",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        for (int i = 0; i < 20; i++)
        {
            delayProvider.RecordedDelays.Clear();
            await engine.TypeProfileAsync(profile, CancellationToken.None);
            Assert.Single(delayProvider.RecordedDelays);
            Assert.InRange(delayProvider.RecordedDelays[0], 100, 500);
        }
    }

    [Fact]
    public async Task ManualScenario3_RepeatCount_DifferentDelaysPerRepetition()
    {
        // 3. Repeat Count > 1 with {WAIT:20-400} — verify each repetition can receive a different delay
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "X{WAIT:20-400}",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 8
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);
        Assert.Equal(8, delayProvider.RecordedDelays.Count);
        foreach (var d in delayProvider.RecordedDelays)
        {
            Assert.InRange(d, 20, 400);
        }
        Assert.True(delayProvider.RecordedDelays.Distinct().Count() > 1, $"Repetitions should receive different delays: {string.Join(", ", delayProvider.RecordedDelays)}");
    }

    [Fact]
    public async Task ManualScenario4_FixedWaitStillWorks()
    {
        // 4. Test {WAIT:500} to confirm fixed WAIT still works
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "A{WAIT:500}B",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);
        Assert.Single(delayProvider.RecordedDelays);
        Assert.Equal(500, delayProvider.RecordedDelays[0]);
    }

    [Fact]
    public async Task ManualScenario5_CancellationDuringLongWait()
    {
        // 5. Test cancellation during a long random WAIT
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);
        using var cts = new CancellationTokenSource();

        delayProvider.OnDelay = (ms) =>
        {
            cts.Cancel();
        };

        var profile = new AutoTypeProfile
        {
            Text = "START{WAIT:5000-10000}END",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        await engine.TypeProfileAsync(profile, cts.Token);
        Assert.Equal(TypingState.Stopped, engine.CurrentState);
        Assert.Equal("START", new string(simulator.TypedCharacters.ToArray()));
    }

    [Fact]
    public async Task ManualScenario6_NormalTypingNoRegression()
    {
        // 6. Confirm no regression to normal typing
        var simulator = new TestKeyboardSimulator();
        var delayProvider = new TestDelayProvider();
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, delayProvider, parser);

        var profile = new AutoTypeProfile
        {
            Text = "Hello, World! 123",
            TypingDelayMs = 15,
            TypingMode = TypingMode.Simulated,
            RepeatCount = 1
        };

        await engine.TypeProfileAsync(profile, CancellationToken.None);
        Assert.Equal(TypingState.Completed, engine.CurrentState);
        Assert.Equal("Hello, World! 123", new string(simulator.TypedCharacters.ToArray()));
        // 17 characters -> 16 inter-character delays
        Assert.Equal(16, delayProvider.RecordedDelays.Count);
        Assert.All(delayProvider.RecordedDelays, d => Assert.Equal(15, d));
    }

    [Fact]
    public async Task ManualScenarioRealTiming_TaskDelayProvider_ExecutesRealPauses()
    {
        var simulator = new TestKeyboardSimulator();
        var realDelayProvider = new TaskDelayProvider(); // REAL Task.Delay
        var parser = new TypingParser();
        var engine = new TypingEngine(simulator, realDelayProvider, parser);

        // Fixed 100ms
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await engine.TypeProfileAsync(new AutoTypeProfile
        {
            Text = "A{WAIT:100}B",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated
        }, CancellationToken.None);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 70, $"Elapsed: {sw.ElapsedMilliseconds}ms");

        // Range 50-150ms
        sw.Restart();
        await engine.TypeProfileAsync(new AutoTypeProfile
        {
            Text = "A{WAIT:50-150}B",
            TypingDelayMs = 0,
            TypingMode = TypingMode.Simulated
        }, CancellationToken.None);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 35, $"Elapsed: {sw.ElapsedMilliseconds}ms");
    }
}
