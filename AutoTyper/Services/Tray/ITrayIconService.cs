using System;
using AutoTyper.Services.Notifications;

namespace AutoTyper.Services.Tray;

/// <summary>
/// Abstraction for system tray icon and context menu management.
/// </summary>
public interface ITrayIconService : IDisposable
{
    /// <summary>
    /// Initializes the tray icon and its context menu callbacks.
    /// </summary>
    void Initialize(Action onShow, Action onStartSelected, Action onStop, Action onExit);

    /// <summary>
    /// Updates the enabled state of the tray context menu items based on application state.
    /// </summary>
    /// <param name="canStartSelected">Whether a profile is selected and ready to start.</param>
    /// <param name="isTyping">Whether typing is currently active.</param>
    void UpdateMenuState(bool canStartSelected, bool isTyping);

    /// <summary>
    /// Displays a notification balloon from the tray icon.
    /// </summary>
    void ShowBalloonTip(string title, string message, NotificationType type, int timeoutMs = 3000);

    /// <summary>
    /// Controls the visibility of the tray icon.
    /// </summary>
    void SetVisible(bool visible);
}
