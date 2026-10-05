using System;

namespace AutoTyper.Services.Hotkeys;

/// <summary>
/// Abstraction for global hotkey registration and management.
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>
    /// Raised when a registered global hotkey is pressed.
    /// The event argument is the hotkey ID.
    /// </summary>
    event EventHandler<int>? HotkeyPressed;

    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    /// <param name="id">A unique integer identifier for this registration.</param>
    /// <param name="hotkey">The parsed hotkey model.</param>
    /// <returns>True if registration succeeded; false if it failed (e.g., already registered by another application).</returns>
    bool Register(int id, HotkeyModel hotkey);

    /// <summary>
    /// Unregisters a previously registered global hotkey by ID.
    /// </summary>
    /// <param name="id">The hotkey registration ID to unregister.</param>
    /// <returns>True if unregistration succeeded.</returns>
    bool Unregister(int id);

    /// <summary>
    /// Unregisters all currently registered hotkeys.
    /// </summary>
    void UnregisterAll();

    /// <summary>
    /// Checks whether a hotkey with the given ID is currently registered.
    /// </summary>
    bool IsRegistered(int id);

    /// <summary>
    /// Checks whether the given hotkey combination conflicts with any already-registered hotkey.
    /// </summary>
    bool HasConflict(HotkeyModel hotkey, int? excludeId = null);
}
