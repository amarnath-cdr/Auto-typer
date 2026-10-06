namespace AutoTyper.Services.Notifications;

public enum NotificationType
{
    Information,
    Warning,
    Error
}

/// <summary>
/// Abstraction for user notifications (e.g. system tray balloon tips or toast notifications).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Displays a notification to the user if notifications are enabled.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message text.</param>
    /// <param name="type">Type of notification (information, warning, error).</param>
    void ShowNotification(string title, string message, NotificationType type = NotificationType.Information);
}
