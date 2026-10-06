using System;
using System.Collections.Generic;

namespace AutoTyper.Services.Engine;

/// <summary>
/// The type of action represented by a parsed typing token.
/// </summary>
public enum TokenType
{
    /// <summary>Ordinary text typed character-by-character via Unicode simulation.</summary>
    Text,

    /// <summary>A single special key press (e.g. {ENTER}, {TAB}, {F5}).</summary>
    SpecialKey,

    /// <summary>A key combination with modifiers (e.g. {CTRL+C}, {ALT+TAB}, {CTRL+SHIFT+S}).</summary>
    KeyCombination
}

/// <summary>
/// Represents an atomic typing action produced by the <see cref="ITypingParser"/>.
/// </summary>
public class TypingToken
{
    public TokenType Type { get; init; }

    /// <summary>Text to type when <see cref="Type"/> is <see cref="TokenType.Text"/>.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>The target virtual key code for special keys and combinations.</summary>
    public ushort VirtualKeyCode { get; init; }

    /// <summary>List of modifier virtual key codes (e.g. VK_CONTROL, VK_SHIFT, VK_MENU, VK_LWIN).</summary>
    public IReadOnlyList<ushort> Modifiers { get; init; } = Array.Empty<ushort>();

    /// <summary>Human-readable token description or tag name.</summary>
    public string Name { get; init; } = string.Empty;

    public static TypingToken CreateText(string text) => new()
    {
        Type = TokenType.Text,
        Text = text,
        Name = "Text"
    };

    public static TypingToken CreateSpecialKey(ushort virtualKeyCode, string name) => new()
    {
        Type = TokenType.SpecialKey,
        VirtualKeyCode = virtualKeyCode,
        Name = name
    };

    public static TypingToken CreateKeyCombination(IReadOnlyList<ushort> modifiers, ushort virtualKeyCode, string name) => new()
    {
        Type = TokenType.KeyCombination,
        Modifiers = modifiers,
        VirtualKeyCode = virtualKeyCode,
        Name = name
    };

    public override string ToString() => Type switch
    {
        TokenType.Text => $"Text(\"{Text}\")",
        TokenType.SpecialKey => $"SpecialKey({Name})",
        TokenType.KeyCombination => $"KeyCombination({Name})",
        _ => base.ToString() ?? string.Empty
    };
}
