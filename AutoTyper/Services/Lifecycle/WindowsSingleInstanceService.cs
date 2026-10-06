using System;
using System.Threading;

namespace AutoTyper.Services.Lifecycle;

/// <summary>
/// Windows single-instance manager using a named <see cref="Mutex"/> and <see cref="EventWaitHandle"/>
/// scoped per user session.
/// </summary>
public class WindowsSingleInstanceService : ISingleInstanceService
{
    private readonly string _mutexName;
    private readonly string _eventName;
    private Mutex? _mutex;
    private EventWaitHandle? _eventWaitHandle;
    private Thread? _listenerThread;
    private Action? _onActivate;
    private bool _hasAcquired;
    private bool _disposed;

    public WindowsSingleInstanceService(string? identifier = null)
    {
        var id = identifier ?? $"AutoTyper_SingleInstance_{Environment.UserName}";
        _mutexName = $"Local\\{id}_Mutex";
        _eventName = $"Local\\{id}_Event";
    }

    /// <inheritdoc />
    public bool Start()
    {
        try
        {
            _mutex = new Mutex(true, _mutexName, out bool createdNew);
            if (!createdNew)
            {
                return false;
            }

            _hasAcquired = true;
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, _eventName);

            _listenerThread = new Thread(ListenForActivation)
            {
                IsBackground = true,
                Name = "SingleInstanceListener"
            };
            _listenerThread.Start();

            return true;
        }
        catch
        {
            // If mutex creation fails (e.g. permission or security issue), allow startup to proceed
            return true;
        }
    }

    /// <inheritdoc />
    public void RegisterActivationCallback(Action onActivate)
    {
        _onActivate = onActivate ?? throw new ArgumentNullException(nameof(onActivate));
    }

    /// <inheritdoc />
    public void SignalExistingInstance()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(_eventName, out var handle))
            {
                using (handle)
                {
                    handle.Set();
                }
            }
        }
        catch
        {
            // Ignore signal errors if existing handle is unavailable
        }
    }

    private void ListenForActivation()
    {
        while (!_disposed && _eventWaitHandle != null)
        {
            try
            {
                if (_eventWaitHandle.WaitOne())
                {
                    if (_disposed)
                        break;

                    _onActivate?.Invoke();
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                // Continue listening
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            try
            {
                _eventWaitHandle?.Set();
                _eventWaitHandle?.Dispose();
                _eventWaitHandle = null;
            }
            catch
            {
                // Ignore cleanup errors
            }

            if (_mutex != null)
            {
                try
                {
                    if (_hasAcquired)
                    {
                        _mutex.ReleaseMutex();
                    }
                    _mutex.Dispose();
                }
                catch
                {
                    // Ignore cleanup errors
                }
                finally
                {
                    _mutex = null;
                }
            }
        }
    }
}
