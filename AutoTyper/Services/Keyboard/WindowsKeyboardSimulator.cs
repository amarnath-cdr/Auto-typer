using System;
using System.Runtime.InteropServices;

namespace AutoTyper.Services.Keyboard;

/// <summary>
/// Windows implementation of <see cref="IKeyboardSimulator"/> using the SendInput API
/// with KEYEVENTF_UNICODE for layout-independent Unicode character input.
/// </summary>
public class WindowsKeyboardSimulator : IKeyboardSimulator
{
    // --- Win32 Constants ---
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    // Virtual key codes for modifier keys
    private const ushort VK_SHIFT = 0x10;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_MENU = 0x12;     // Alt
    private const ushort VK_LWIN = 0x5B;
    private const ushort VK_RWIN = 0x5C;
    private const ushort VK_LSHIFT = 0xA0;
    private const ushort VK_RSHIFT = 0xA1;
    private const ushort VK_LCONTROL = 0xA2;
    private const ushort VK_RCONTROL = 0xA3;
    private const ushort VK_LMENU = 0xA4;
    private const ushort VK_RMENU = 0xA5;
    private const ushort VK_RETURN = 0x0D;

    // --- Win32 Structures ---

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    // --- Win32 P/Invoke ---

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <inheritdoc />
    public void SendCharacter(char character)
    {
        // Handle newline characters by simulating Enter key press
        if (character == '\n' || character == '\r')
        {
            SendKeyPress(VK_RETURN);
            return;
        }

        // For tab, send the Unicode character directly
        var inputs = new INPUT[2];

        // Key down
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = character,
                    dwFlags = KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Key up
        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = character,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        var result = SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        if (result == 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"SendInput failed for character '{character}' (U+{(int)character:X4}). Win32 error: {error}");
        }
    }

    /// <inheritdoc />
    public void SendKeyPress(ushort virtualKeyCode)
    {
        var inputs = new INPUT[2];

        // Key down
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Key up
        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    /// <inheritdoc />
    public void ReleaseAllModifiers()
    {
        ushort[] modifierKeys =
        [
            VK_SHIFT, VK_CONTROL, VK_MENU,
            VK_LSHIFT, VK_RSHIFT,
            VK_LCONTROL, VK_RCONTROL,
            VK_LMENU, VK_RMENU,
            VK_LWIN, VK_RWIN
        ];

        var inputs = new INPUT[modifierKeys.Length];
        for (int i = 0; i < modifierKeys.Length; i++)
        {
            inputs[i] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = modifierKeys[i],
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
