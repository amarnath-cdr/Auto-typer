using System;
using System.Threading;
using System.Threading.Tasks;
using AutoTyper.Services.Keyboard;

namespace AutoTyper.Services.Engine;

/// <summary>
/// Typing engine that sends text character-by-character through an <see cref="IKeyboardSimulator"/>
/// with configurable constant or randomized (jitter) delay between keystrokes.
/// </summary>
public class TypingEngine : ITypingEngine
{
    private readonly IKeyboardSimulator _keyboardSimulator;
    private readonly IDelayProvider _delayProvider;
    private readonly Random _random = new();

    private TypingState _currentState = TypingState.Ready;

    public TypingEngine(IKeyboardSimulator keyboardSimulator, IDelayProvider delayProvider)
    {
        _keyboardSimulator = keyboardSimulator ?? throw new ArgumentNullException(nameof(keyboardSimulator));
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

    /// <inheritdoc />
    public TypingState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                StateChanged?.Invoke(this, value);
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<TypingState>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<TypingProgress>? ProgressChanged;

    /// <inheritdoc />
    public async Task TypeTextAsync(string text, int delayMs, bool useJitter, int minDelayMs, int maxDelayMs,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            CurrentState = TypingState.Completed;
            return;
        }

        CurrentState = TypingState.Typing;

        try
        {
            // Normalize \r\n to \n to avoid double-newlines, then process each character
            var normalized = text.Replace("\r\n", "\n");
            int total = normalized.Length;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                char c = normalized[i];
                _keyboardSimulator.SendCharacter(c);

                ProgressChanged?.Invoke(this, new TypingProgress
                {
                    CurrentIndex = i + 1,
                    TotalCharacters = total
                });

                // Apply delay between characters (skip after the last character)
                if (i < total - 1)
                {
                    int delay = useJitter
                        ? _random.Next(Math.Min(minDelayMs, maxDelayMs), Math.Max(minDelayMs, maxDelayMs) + 1)
                        : delayMs;

                    await _delayProvider.DelayAsync(delay, cancellationToken);
                }
            }

            CurrentState = TypingState.Completed;
        }
        catch (OperationCanceledException)
        {
            _keyboardSimulator.ReleaseAllModifiers();
            CurrentState = TypingState.Stopped;
        }
        catch (Exception)
        {
            _keyboardSimulator.ReleaseAllModifiers();
            CurrentState = TypingState.Error;
            throw;
        }
    }
}
