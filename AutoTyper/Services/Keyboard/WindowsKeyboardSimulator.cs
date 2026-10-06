using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AutoTyper.Services.Keyboard;

/// <summary>
/// Windows implementation of <see cref="IKeyboardSimulator"/> using the SendInput API
/// with KEYEVENTF_UNICODE for layout-independent Unicode character input and
/// native virtual key codes for special keys and modifier combinations.
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
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

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
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
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

        // Send Unicode character key-down and key-up
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

        var result = SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        if (result == 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"SendInput failed for virtual key 0x{virtualKeyCode:X2}. Win32 error: {error}");
        }
    }

    /// <inheritdoc />
    public void SendKeyDown(ushort virtualKeyCode)
    {
        var inputs = new INPUT[1];
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

        var result = SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        if (result == 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"SendInput key-down failed for virtual key 0x{virtualKeyCode:X2}. Win32 error: {error}");
        }
    }

    /// <inheritdoc />
    public void SendKeyUp(ushort virtualKeyCode)
    {
        var inputs = new INPUT[1];
        inputs[0] = new INPUT
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

        var result = SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        if (result == 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"SendInput key-up failed for virtual key 0x{virtualKeyCode:X2}. Win32 error: {error}");
        }
    }

    /// <inheritdoc />
    public void SendKeyCombination(IReadOnlyList<ushort> modifiers, ushort targetKey)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        // Total events = modifiers.Count (down) + 2 (target down/up) + modifiers.Count (up)
        int totalInputs = (modifiers.Count * 2) + 2;
        var inputs = new INPUT[totalInputs];
        int index = 0;

        // 1. Modifiers Down
        for (int i = 0; i < modifiers.Count; i++)
        {
            inputs[index++] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = modifiers[i],
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        // 2. Target Key Down
        inputs[index++] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = targetKey,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // 3. Target Key Up
        inputs[index++] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = targetKey,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // 4. Modifiers Up in reverse order
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            inputs[index++] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = modifiers[i],
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        try
        {
            var result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            if (result == 0)
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    $"SendInput failed for key combination with target key 0x{targetKey:X2}. Win32 error: {error}");
            }
        }
        finally
        {
            // Safety: if anything failed, release modifiers
            ReleaseAllModifiers();
        }
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
