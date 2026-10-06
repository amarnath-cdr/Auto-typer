using System.Collections.Generic;

namespace AutoTyper.Services.Engine;

/// <summary>
/// Abstraction for parsing profile text templates into sequential typing tokens.
/// </summary>
public interface ITypingParser
{
    /// <summary>
    /// Parses the raw profile input text into a list of <see cref="TypingToken"/> actions.
    /// Handles special key tokens (e.g. {ENTER}), modifier combinations (e.g. {CTRL+C}),
    /// literal braces ({{ and }}), and standard text.
    /// </summary>
    /// <param name="rawText">The raw input text to parse.</param>
    /// <returns>A read-only list of parsed typing tokens.</returns>
    IReadOnlyList<TypingToken> Parse(string? rawText);
}
