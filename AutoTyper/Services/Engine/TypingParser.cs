using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTyper.Services.Engine;

/// <summary>
/// Parser that converts profile text templates into sequential typing tokens.
/// Supports special-key tokens ({ENTER}, {TAB}, etc.), modifier combinations ({CTRL+C}, {ALT+TAB}, etc.),
/// literal brace escaping ({{ and }}), and standard text.
/// </summary>
public class TypingParser : ITypingParser
{
    private static readonly Dictionary<string, ushort> SpecialKeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common navigation and editing keys
        { "ENTER", 0x0D },
        { "RETURN", 0x0D },
        { "TAB", 0x09 },
        { "BACKSPACE", 0x08 },
        { "BACK", 0x08 },
        { "SPACE", 0x20 },
        { "ESC", 0x1B },
        { "ESCAPE", 0x1B },
        { "UP", 0x26 },
        { "DOWN", 0x28 },
        { "LEFT", 0x25 },
        { "RIGHT", 0x27 },
        { "HOME", 0x24 },
        { "END", 0x23 },
        { "DELETE", 0x2E },
        { "DEL", 0x2E },
        { "INSERT", 0x2D },
        { "INS", 0x2D },
        { "PAGEUP", 0x21 },
        { "PGUP", 0x21 },
        { "PAGEDOWN", 0x22 },
        { "PGDN", 0x22 },

        // Locks and system keys
        { "CAPSLOCK", 0x14 },
        { "NUMLOCK", 0x90 },
        { "SCROLLLOCK", 0x91 },
        { "PRINTSCREEN", 0x2C },
        { "PRTSC", 0x2C },
        { "PAUSE", 0x13 },
        { "BREAK", 0x13 },

        // Function keys F1-F12
        { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
        { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
        { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B }
    };

    private static readonly Dictionary<string, ushort> ModifierMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "CTRL", 0x11 },
        { "CONTROL", 0x11 },
        { "SHIFT", 0x10 },
        { "ALT", 0x12 },
        { "MENU", 0x12 },
        { "WIN", 0x5B },
        { "WINDOWS", 0x5B }
    };

    private static readonly Dictionary<string, ushort> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Letters A-Z
        { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 },
        { "E", 0x45 }, { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 },
        { "I", 0x49 }, { "J", 0x4A }, { "K", 0x4B }, { "L", 0x4C },
        { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F }, { "P", 0x50 },
        { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
        { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 },
        { "Y", 0x59 }, { "Z", 0x5A },

        // Digits 0-9
        { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 },
        { "4", 0x34 }, { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 },
        { "8", 0x38 }, { "9", 0x39 }
    };

    /// <inheritdoc />
    public IReadOnlyList<TypingToken> Parse(string? rawText)
    {
        var tokens = new List<TypingToken>();
        if (string.IsNullOrEmpty(rawText))
            return tokens;

        var textBuffer = new StringBuilder();
        int i = 0;
        int len = rawText.Length;

        while (i < len)
        {
            // Check for escaped opening brace: {{ -> {
            if (rawText[i] == '{' && i + 1 < len && rawText[i + 1] == '{')
            {
                textBuffer.Append('{');
                i += 2;
                continue;
            }

            // Check for escaped closing brace: }} -> }
            if (rawText[i] == '}' && i + 1 < len && rawText[i + 1] == '}')
            {
                textBuffer.Append('}');
                i += 2;
                continue;
            }

            // Potential token start
            if (rawText[i] == '{')
            {
                int closeIndex = rawText.IndexOf('}', i + 1);
                if (closeIndex != -1)
                {
                    string candidate = rawText.Substring(i + 1, closeIndex - i - 1).Trim();
                    if (TryParseToken(candidate, out var token) && token != null)
                    {
                        // Flush any text accumulated before this token
                        if (textBuffer.Length > 0)
                        {
                            tokens.Add(TypingToken.CreateText(textBuffer.ToString()));
                            textBuffer.Clear();
                        }

                        tokens.Add(token);
                        i = closeIndex + 1;
                        continue;
                    }
                }
            }

            // Regular character
            textBuffer.Append(rawText[i]);
            i++;
        }

        // Flush remaining text
        if (textBuffer.Length > 0)
        {
            tokens.Add(TypingToken.CreateText(textBuffer.ToString()));
        }

        return tokens;
    }

    private static bool TryParseToken(string content, out TypingToken? token)
    {
        token = null;
        if (string.IsNullOrWhiteSpace(content))
            return false;

        // 1. Check if it's a single special key (e.g. "ENTER", "TAB", "F7")
        if (SpecialKeyMap.TryGetValue(content, out ushort specialVk))
        {
            token = TypingToken.CreateSpecialKey(specialVk, content.ToUpperInvariant());
            return true;
        }

        // 2. Check if it's a modifier combination (e.g. "CTRL+C", "ALT+TAB", "CTRL+SHIFT+S")
        if (content.Contains('+'))
        {
            var parts = content.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                var modifiers = new List<ushort>();
                bool allModifiersValid = true;

                // All parts except the last one MUST be valid modifiers
                for (int p = 0; p < parts.Length - 1; p++)
                {
                    if (ModifierMap.TryGetValue(parts[p], out ushort modVk))
                    {
                        if (!modifiers.Contains(modVk))
                        {
                            modifiers.Add(modVk);
                        }
                    }
                    else
                    {
                        allModifiersValid = false;
                        break;
                    }
                }

                if (allModifiersValid && modifiers.Count > 0)
                {
                    string targetKeyPart = parts[^1];
                    if (TryParseTargetKey(targetKeyPart, out ushort targetVk))
                    {
                        token = TypingToken.CreateKeyCombination(
                            modifiers,
                            targetVk,
                            string.Join("+", parts).ToUpperInvariant());
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TryParseTargetKey(string keyPart, out ushort vk)
    {
        // Check letters and digits
        if (KeyMap.TryGetValue(keyPart, out vk))
            return true;

        // Check special keys (e.g. TAB, ENTER, ESC, F1-F12, DELETE)
        if (SpecialKeyMap.TryGetValue(keyPart, out vk))
            return true;

        return false;
    }
}
