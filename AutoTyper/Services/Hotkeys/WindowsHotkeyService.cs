using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace AutoTyper.Services.Hotkeys;

/// <summary>
/// Windows implementation of <see cref="IHotkeyService"/> using RegisterHotKey/UnregisterHotKey
/// and an HwndSource message hook to receive WM_HOTKEY messages.
/// </summary>
public class WindowsHotkeyService : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Dictionary<int, HotkeyModel> _registeredHotkeys = new();
    private HwndSource? _hwndSource;
    private IntPtr _windowHandle;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<int>? HotkeyPressed;

    /// <summary>
    /// Initializes the service by attaching to the given WPF window handle.
    /// Must be called after the window has been loaded and has a valid HWND.
    /// </summary>
    /// <param name="windowHandle">The HWND of the WPF main window.</param>
    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource?.AddHook(WndProc);
    }

    /// <inheritdoc />
    public bool Register(int id, HotkeyModel hotkey)
    {
        ArgumentNullException.ThrowIfNull(hotkey);

        if (_windowHandle == IntPtr.Zero)
            throw new InvalidOperationException("HotkeyService has not been initialized with a window handle.");

        if (_registeredHotkeys.ContainsKey(id))
        {
            // Unregister existing before re-registering
            Unregister(id);
        }

        bool success = RegisterHotKey(_windowHandle, id, hotkey.Modifiers, hotkey.VirtualKeyCode);
        if (success)
        {
            _registeredHotkeys[id] = hotkey;
        }

        return success;
    }

    /// <inheritdoc />
    public bool Unregister(int id)
    {
        if (_windowHandle == IntPtr.Zero)
            return false;

        bool success = UnregisterHotKey(_windowHandle, id);
        _registeredHotkeys.Remove(id);
        return success;
    }

    /// <inheritdoc />
    public void UnregisterAll()
    {
        foreach (var id in _registeredHotkeys.Keys.ToList())
        {
            Unregister(id);
        }
    }

    /// <inheritdoc />
    public bool IsRegistered(int id) => _registeredHotkeys.ContainsKey(id);

    /// <inheritdoc />
    public bool HasConflict(HotkeyModel hotkey, int? excludeId = null)
    {
        ArgumentNullException.ThrowIfNull(hotkey);
        var uniqueKey = hotkey.GetUniqueKey();

        foreach (var kvp in _registeredHotkeys)
        {
            if (excludeId.HasValue && kvp.Key == excludeId.Value)
                continue;

            if (kvp.Value.GetUniqueKey() == uniqueKey)
                return true;
        }

        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            if (_registeredHotkeys.ContainsKey(hotkeyId))
            {
                HotkeyPressed?.Invoke(this, hotkeyId);
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterAll();
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource = null;
            _disposed = true;
        }
    }
}
