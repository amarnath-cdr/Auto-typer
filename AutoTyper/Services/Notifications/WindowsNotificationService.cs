using System;
using AutoTyper.Services.Tray;

namespace AutoTyper.Services.Notifications;

/// <summary>
/// Windows notification service that dispatches notifications to the system tray icon
/// based on the user's notification preferences.
/// </summary>
public class WindowsNotificationService : INotificationService
{
    private readonly ITrayIconService _trayIconService;
    private readonly ISettingsStorageService _settingsStorage;

    public WindowsNotificationService(ITrayIconService trayIconService, ISettingsStorageService settingsStorage)
    {
        _trayIconService = trayIconService ?? throw new ArgumentNullException(nameof(trayIconService));
        _settingsStorage = settingsStorage ?? throw new ArgumentNullException(nameof(settingsStorage));
    }

    /// <inheritdoc />
    public void ShowNotification(string title, string message, NotificationType type = NotificationType.Information)
    {
        try
        {
            var settings = _settingsStorage.LoadSettings();
            if (!settings.ShowNotifications)
                return;

            _trayIconService.ShowBalloonTip(title, message, type);
        }
        catch
        {
            // Fail silently so notifications never crash the app
        }
    }
}
