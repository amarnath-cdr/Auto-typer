using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace AutoTyper.Services.Startup;

/// <summary>
/// Windows per-user registry implementation of <see cref="IStartupService"/>
/// using HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// Does not require administrative privileges.
/// </summary>
public class WindowsStartupService : IStartupService
{
    private const string RunRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "AutoTyper";

    /// <inheritdoc />
    public bool IsStartWithWindowsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: false);
            if (key == null)
                return false;

            var value = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetStartWithWindows(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: true);
            if (key == null)
                return false;

            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                    return false;

                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
