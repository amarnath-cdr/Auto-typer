using System;
using System.Threading;
using System.Threading.Tasks;
using AutoTyper.Models;
using AutoTyper.Services.Clipboard;
using AutoTyper.Services.Keyboard;

namespace AutoTyper.Services.Engine;

/// <summary>
/// Typing engine that simulates keyboard input (character-by-character or via clipboard)
/// with support for special-key tokens, modifier combinations, start delays, and repeat loops.
/// </summary>
public class TypingEngine : ITypingEngine
{
    private readonly IKeyboardSimulator _keyboardSimulator;
    private readonly IDelayProvider _delayProvider;
    private readonly ITypingParser _typingParser;
    private readonly IClipboardService _clipboardService;
    private readonly Random _random = new();

    private TypingState _currentState = TypingState.Ready;

    public TypingEngine(
        IKeyboardSimulator keyboardSimulator,
        IDelayProvider delayProvider,
        ITypingParser? typingParser = null,
        IClipboardService? clipboardService = null)
    {
        _keyboardSimulator = keyboardSimulator ?? throw new ArgumentNullException(nameof(keyboardSimulator));
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
        _typingParser = typingParser ?? new TypingParser();
        _clipboardService = clipboardService ?? new WindowsClipboardService();
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
    public Task TypeTextAsync(string text, int delayMs, bool useJitter, int minDelayMs, int maxDelayMs,
        CancellationToken cancellationToken)
    {
        var profile = new AutoTypeProfile
        {
            Text = text,
            TypingDelayMs = delayMs,
            UseJitter = useJitter,
            MinDelayMs = minDelayMs,
            MaxDelayMs = maxDelayMs,
            StartDelayMs = 0,
            RepeatCount = 1,
            TypingMode = TypingMode.Simulated,
            Capitalization = CapitalizationMode.Original
        };

        return TypeProfileAsync(profile, cancellationToken);
    }

    /// <inheritdoc />
    public async Task TypeProfileAsync(AutoTypeProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        CurrentState = TypingState.Typing;

        try
        {
            // 1. Initial start delay if specified
            if (profile.StartDelayMs > 0)
            {
                await _delayProvider.DelayAsync(profile.StartDelayMs, cancellationToken);
            }

            if (string.IsNullOrEmpty(profile.Text))
            {
                CurrentState = TypingState.Completed;
                return;
            }

            // 2. Execute according to TypingMode
            if (profile.TypingMode == TypingMode.Clipboard)
            {
                await ExecuteClipboardModeAsync(profile, cancellationToken);
            }
            else
            {
                await ExecuteSimulatedModeAsync(profile, cancellationToken);
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

    private async Task ExecuteClipboardModeAsync(AutoTypeProfile profile, CancellationToken cancellationToken)
    {
        string textToPaste = ApplyCapitalization(profile.Text ?? string.Empty, profile.Capitalization);
        int repeats = Math.Max(1, profile.RepeatCount);

        for (int r = 0; r < repeats; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? savedClipboard = null;
            try
            {
                savedClipboard = _clipboardService.GetText();
            }
            catch
            {
                // Ignore failure reading initial clipboard
            }

            try
            {
                _clipboardService.SetText(textToPaste);

                // Small delay for clipboard propagation
                await _delayProvider.DelayAsync(20, cancellationToken);

                // Simulate Ctrl+V: VK_CONTROL = 0x11, 'V' = 0x56
                _keyboardSimulator.SendKeyCombination(new ushort[] { 0x11 }, 0x56);

                // Small delay to allow target application to consume clipboard
                await _delayProvider.DelayAsync(50, cancellationToken);
            }
            finally
            {
                try
                {
                    if (savedClipboard != null)
                    {
                        _clipboardService.SetText(savedClipboard);
                    }
                    else
                    {
                        _clipboardService.Clear();
                    }
                }
                catch
                {
                    // Ignore failure restoring clipboard
                }
            }

            ProgressChanged?.Invoke(this, new TypingProgress
            {
                CurrentIndex = r + 1,
                TotalCharacters = repeats
            });

            if (r < repeats - 1)
            {
                int delay = profile.TypingDelayMs > 0 ? profile.TypingDelayMs : 20;
                await _delayProvider.DelayAsync(delay, cancellationToken);
            }
        }
    }

    private async Task ExecuteSimulatedModeAsync(AutoTypeProfile profile, CancellationToken cancellationToken)
    {
        string rawText = profile.Text ?? string.Empty;
        var tokens = _typingParser.Parse(rawText);
        int repeats = Math.Max(1, profile.RepeatCount);

        // Calculate total steps per repetition
        int stepsPerRep = 0;
        foreach (var token in tokens)
        {
            stepsPerRep += token.Type == TokenType.Text ? token.Text.Length : 1;
        }

        if (stepsPerRep == 0)
            return;

        int totalSteps = stepsPerRep * repeats;
        int currentStep = 0;

        for (int r = 0; r < repeats; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int t = 0; t < tokens.Count; t++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var token = tokens[t];

                switch (token.Type)
                {
                    case TokenType.Text:
                        string text = ApplyCapitalization(token.Text, profile.Capitalization);
                        for (int i = 0; i < text.Length; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            char c = text[i];
                            _keyboardSimulator.SendCharacter(c);

                            currentStep++;
                            ProgressChanged?.Invoke(this, new TypingProgress
                            {
                                CurrentIndex = currentStep,
                                TotalCharacters = totalSteps
                            });

                            if (currentStep < totalSteps)
                            {
                                int delay = CalculateDelay(profile);
                                if (delay > 0)
                                {
                                    await _delayProvider.DelayAsync(delay, cancellationToken);
                                }
                            }
                        }
                        break;

                    case TokenType.SpecialKey:
                        _keyboardSimulator.SendKeyPress(token.VirtualKeyCode);
                        currentStep++;
                        ProgressChanged?.Invoke(this, new TypingProgress
                        {
                            CurrentIndex = currentStep,
                            TotalCharacters = totalSteps
                        });

                        if (currentStep < totalSteps)
                        {
                            int delay = CalculateDelay(profile);
                            if (delay > 0)
                            {
                                await _delayProvider.DelayAsync(delay, cancellationToken);
                            }
                        }
                        break;

                    case TokenType.KeyCombination:
                        _keyboardSimulator.SendKeyCombination(token.Modifiers, token.VirtualKeyCode);
                        currentStep++;
                        ProgressChanged?.Invoke(this, new TypingProgress
                        {
                            CurrentIndex = currentStep,
                            TotalCharacters = totalSteps
                        });

                        if (currentStep < totalSteps)
                        {
                            int delay = CalculateDelay(profile);
                            if (delay > 0)
                            {
                                await _delayProvider.DelayAsync(delay, cancellationToken);
                            }
                        }
                        break;

                    case TokenType.Wait:
                        if (token.WaitMilliseconds > 0)
                        {
                            await _delayProvider.DelayAsync(token.WaitMilliseconds, cancellationToken);
                        }
                        currentStep++;
                        ProgressChanged?.Invoke(this, new TypingProgress
                        {
                            CurrentIndex = currentStep,
                            TotalCharacters = totalSteps
                        });
                        break;
                }
            }
        }
    }

    private int CalculateDelay(AutoTypeProfile profile)
    {
        if (profile.UseJitter)
        {
            int min = Math.Min(profile.MinDelayMs, profile.MaxDelayMs);
            int max = Math.Max(profile.MinDelayMs, profile.MaxDelayMs);
            return _random.Next(min, max + 1);
        }

        return profile.TypingDelayMs;
    }

    private static string ApplyCapitalization(string text, CapitalizationMode mode) => mode switch
    {
        CapitalizationMode.Uppercase => text.ToUpperInvariant(),
        CapitalizationMode.Lowercase => text.ToLowerInvariant(),
        CapitalizationMode.SentenceCase => ToSentenceCase(text),
        _ => text
    };

    private static string ToSentenceCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var chars = text.ToCharArray();
        bool newSentence = true;

        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsLetter(chars[i]))
            {
                if (newSentence)
                {
                    chars[i] = char.ToUpperInvariant(chars[i]);
                    newSentence = false;
                }
                else
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }
            }
            else if (chars[i] is '.' or '!' or '?')
            {
                newSentence = true;
            }
        }

        return new string(chars);
    }
}
