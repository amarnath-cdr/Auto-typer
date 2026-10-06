using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace AutoTyper.Services.Clipboard;

/// <summary>
/// Windows implementation of <see cref="IClipboardService"/> using WPF <see cref="System.Windows.Clipboard"/>
/// with retry handling for transient clipboard locks.
/// </summary>
public class WindowsClipboardService : IClipboardService
{
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 25;

    /// <inheritdoc />
    public string? GetText()
    {
        return RunOnSta(() =>
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        return System.Windows.Clipboard.GetText();
                    }
                    return null;
                }
                catch (COMException)
                {
                    if (i == MaxRetries - 1) break;
                    Thread.Sleep(RetryDelayMs);
                }
            }
            return null;
        });
    }

    /// <inheritdoc />
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        RunOnSta(() =>
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    System.Windows.Clipboard.SetDataObject(text, true);
                    return true;
                }
                catch (COMException)
                {
                    if (i == MaxRetries - 1) break;
                    Thread.Sleep(RetryDelayMs);
                }
            }
            return false;
        });
    }

    /// <inheritdoc />
    public void Clear()
    {
        RunOnSta(() =>
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    System.Windows.Clipboard.Clear();
                    return true;
                }
                catch (COMException)
                {
                    if (i == MaxRetries - 1) break;
                    Thread.Sleep(RetryDelayMs);
                }
            }
            return false;
        });
    }

    private static T RunOnSta<T>(Func<T> action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            return action();
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.HasShutdownStarted)
        {
            return Application.Current.Dispatcher.Invoke(action);
        }

        T result = default!;
        var thread = new Thread(() =>
        {
            result = action();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }
}
