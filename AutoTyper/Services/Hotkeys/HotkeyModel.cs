using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace AutoTyper.Services.Hotkeys;

/// <summary>
/// Represents a parsed global hotkey combination (modifiers + key).
/// </summary>
public class HotkeyModel
{
    /// <summary>Win32 modifier flags for RegisterHotKey.</summary>
    public uint Modifiers { get; init; }

    /// <summary>Win32 virtual-key code.</summary>
    public uint VirtualKeyCode { get; init; }

    /// <summary>Original string representation (e.g. "Ctrl+Shift+F7").</summary>
    public string DisplayString { get; init; } = string.Empty;

    // Win32 modifier flag constants for RegisterHotKey
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    /// <summary>
    /// Attempts to parse a hotkey string such as "F7", "Ctrl+F8", "Alt+Shift+T", or "Escape".
    /// </summary>
    /// <param name="hotkeyString">The hotkey string to parse.</param>
    /// <param name="result">The parsed HotkeyModel if successful.</param>
    /// <returns>True if parsing succeeded; false otherwise.</returns>
    public static bool TryParse(string? hotkeyString, out HotkeyModel? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(hotkeyString))
            return false;

        var parts = hotkeyString.Trim().Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        uint modifiers = MOD_NOREPEAT; // Always prevent auto-repeat
        string keyPart = parts[^1]; // Last part is the key

        // Parse modifier prefixes
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var mod = parts[i].ToUpperInvariant();
            switch (mod)
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= MOD_CONTROL;
                    break;
                case "ALT":
                    modifiers |= MOD_ALT;
                    break;
                case "SHIFT":
                    modifiers |= MOD_SHIFT;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    return false; // Unknown modifier
            }
        }

        // Parse the key using WPF Key enum
        if (!TryParseVirtualKeyCode(keyPart, out uint vk))
            return false;

        result = new HotkeyModel
        {
            Modifiers = modifiers,
            VirtualKeyCode = vk,
            DisplayString = hotkeyString.Trim()
        };

        return true;
    }

    /// <summary>
    /// Returns a unique identifier for this hotkey combination (used for duplicate detection).
    /// </summary>
    public string GetUniqueKey() => $"{Modifiers & ~MOD_NOREPEAT}:{VirtualKeyCode}";

    public override string ToString() => DisplayString;

    public override bool Equals(object? obj)
    {
        if (obj is HotkeyModel other)
        {
            return (Modifiers & ~MOD_NOREPEAT) == (other.Modifiers & ~MOD_NOREPEAT)
                   && VirtualKeyCode == other.VirtualKeyCode;
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Modifiers & ~MOD_NOREPEAT, VirtualKeyCode);

    // --- Key name to virtual key code mapping ---

    private static readonly Dictionary<string, uint> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Function keys
        { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
        { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
        { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },

        // Common control keys
        { "ESCAPE", 0x1B }, { "ESC", 0x1B },
        { "TAB", 0x09 },
        { "SPACE", 0x20 },
        { "ENTER", 0x0D }, { "RETURN", 0x0D },
        { "BACKSPACE", 0x08 }, { "BACK", 0x08 },
        { "DELETE", 0x2E }, { "DEL", 0x2E },
        { "INSERT", 0x2D }, { "INS", 0x2D },
        { "HOME", 0x24 }, { "END", 0x23 },
        { "PAGEUP", 0x21 }, { "PGUP", 0x21 },
        { "PAGEDOWN", 0x22 }, { "PGDN", 0x22 },
        { "PAUSE", 0x13 }, { "BREAK", 0x13 },
        { "PRINTSCREEN", 0x2C }, { "PRTSC", 0x2C },
        { "SCROLLLOCK", 0x91 },
        { "CAPSLOCK", 0x14 }, { "NUMLOCK", 0x90 },

        // Arrow keys
        { "LEFT", 0x25 }, { "UP", 0x26 }, { "RIGHT", 0x27 }, { "DOWN", 0x28 },

        // Letters A-Z
        { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 },
        { "E", 0x45 }, { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 },
        { "I", 0x49 }, { "J", 0x4A }, { "K", 0x4B }, { "L", 0x4C },
        { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F }, { "P", 0x50 },
        { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
        { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 },
        { "Y", 0x59 }, { "Z", 0x5A },

        // Number keys 0-9
        { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 },
        { "4", 0x34 }, { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 },
        { "8", 0x38 }, { "9", 0x39 }
    };

    private static bool TryParseVirtualKeyCode(string keyName, out uint virtualKeyCode)
    {
        return KeyMap.TryGetValue(keyName, out virtualKeyCode);
    }
}
