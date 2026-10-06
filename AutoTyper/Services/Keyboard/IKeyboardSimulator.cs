using System.Collections.Generic;

namespace AutoTyper.Services.Keyboard;

/// <summary>
/// Abstraction for keyboard input simulation.
/// Implementations send keystrokes to the active window.
/// </summary>
public interface IKeyboardSimulator
{
    /// <summary>
    /// Sends a single Unicode character as a keystroke (key-down + key-up).
    /// </summary>
    /// <param name="character">The Unicode character to type.</param>
    void SendCharacter(char character);

    /// <summary>
    /// Sends a virtual key press (key-down + key-up), such as VK_RETURN.
    /// </summary>
    /// <param name="virtualKeyCode">The Win32 virtual-key code.</param>
    void SendKeyPress(ushort virtualKeyCode);

    /// <summary>
    /// Sends a key-down event for the specified virtual key code.
    /// </summary>
    void SendKeyDown(ushort virtualKeyCode);

    /// <summary>
    /// Sends a key-up event for the specified virtual key code.
    /// </summary>
    void SendKeyUp(ushort virtualKeyCode);

    /// <summary>
    /// Sends a key combination: presses modifiers down in order, taps the target key, and releases modifiers in reverse order.
    /// </summary>
    /// <param name="modifiers">The modifier virtual key codes to hold down.</param>
    /// <param name="targetKey">The target virtual key code to press.</param>
    void SendKeyCombination(IReadOnlyList<ushort> modifiers, ushort targetKey);

    /// <summary>
    /// Releases all modifier keys (Shift, Ctrl, Alt, Win) to prevent stuck keys.
    /// Must be called on cancellation or error to leave the keyboard in a clean state.
    /// </summary>
    void ReleaseAllModifiers();
}
