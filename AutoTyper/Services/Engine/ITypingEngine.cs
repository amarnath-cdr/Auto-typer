using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTyper.Services.Engine;

/// <summary>
/// Progress information for a typing operation.
/// </summary>
public class TypingProgress
{
    public int CurrentIndex { get; init; }
    public int TotalCharacters { get; init; }
    public double PercentComplete => TotalCharacters > 0 ? (double)CurrentIndex / TotalCharacters * 100 : 0;
}

/// <summary>
/// Abstraction for the typing engine that simulates text input character-by-character.
/// </summary>
public interface ITypingEngine
{
    /// <summary>
    /// Current state of the typing engine.
    /// </summary>
    TypingState CurrentState { get; }

    /// <summary>
    /// Raised when the engine state changes.
    /// </summary>
    event EventHandler<TypingState>? StateChanged;

    /// <summary>
    /// Raised when typing progress changes (after each character).
    /// </summary>
    event EventHandler<TypingProgress>? ProgressChanged;

    /// <summary>
    /// Types the given text character-by-character with the specified timing.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="delayMs">Base delay between characters in milliseconds.</param>
    /// <param name="useJitter">Whether to randomize delay.</param>
    /// <param name="minDelayMs">Minimum random delay (when jitter is enabled).</param>
    /// <param name="maxDelayMs">Maximum random delay (when jitter is enabled).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task TypeTextAsync(string text, int delayMs, bool useJitter, int minDelayMs, int maxDelayMs,
        CancellationToken cancellationToken);
}
