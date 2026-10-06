using System;

namespace AutoTyper.Services.Lifecycle;

/// <summary>
/// Abstraction for enforcing a single instance of the application per user session.
/// </summary>
public interface ISingleInstanceService : IDisposable
{
    /// <summary>
    /// Attempts to acquire the single-instance lock.
    /// </summary>
    /// <returns>True if this is the first instance; false if another instance is already running.</returns>
    bool Start();

    /// <summary>
    /// Registers an action to execute when another instance requests activation.
    /// </summary>
    void RegisterActivationCallback(Action onActivate);

    /// <summary>
    /// Signals the primary running instance to activate and bring its window to the foreground.
    /// </summary>
    void SignalExistingInstance();
}
