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
    /// Releases all modifier keys (Shift, Ctrl, Alt, Win) to prevent stuck keys.
    /// Must be called on cancellation or error to leave the keyboard in a clean state.
    /// </summary>
    void ReleaseAllModifiers();
}
