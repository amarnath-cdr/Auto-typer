using System;
using System.Collections.Generic;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Services.Notifications;
using AutoTyper.Services.Tray;
using Xunit;

namespace AutoTyper.Tests;

public class NotificationServiceTests
{
    private class MockTrayIconService : ITrayIconService
    {
        public List<(string Title, string Message, NotificationType Type)> BalloonTips { get; } = new();

        public void Initialize(Action onShow, Action onStartSelected, Action onStop, Action onExit) { }
        public void UpdateMenuState(bool canStartSelected, bool isTyping) { }
        public void ShowBalloonTip(string title, string message, NotificationType type, int timeoutMs = 3000)
        {
            BalloonTips.Add((title, message, type));
        }
        public void SetVisible(bool visible) { }
        public void Dispose() { }
    }

    private class MockSettingsStorage : ISettingsStorageService
    {
        public AppSettings Settings { get; set; } = new();
        public AppSettings LoadSettings() => Settings;
        public void SaveSettings(AppSettings settings) => Settings = settings;
        public System.Threading.Tasks.Task<AppSettings> LoadSettingsAsync() => System.Threading.Tasks.Task.FromResult(Settings);
        public System.Threading.Tasks.Task SaveSettingsAsync(AppSettings settings)
        {
            Settings = settings;
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    [Fact]
    public void ShowNotification_WhenEnabled_DispatchesToTray()
    {
        var tray = new MockTrayIconService();
        var storage = new MockSettingsStorage { Settings = new AppSettings { ShowNotifications = true } };
        var service = new WindowsNotificationService(tray, storage);

        service.ShowNotification("AutoTyper", "Typing started: Greeting", NotificationType.Information);

        Assert.Single(tray.BalloonTips);
        Assert.Equal("AutoTyper", tray.BalloonTips[0].Title);
        Assert.Equal("Typing started: Greeting", tray.BalloonTips[0].Message);
    }

    [Fact]
    public void ShowNotification_WhenDisabled_SuppressesNotification()
    {
        var tray = new MockTrayIconService();
        var storage = new MockSettingsStorage { Settings = new AppSettings { ShowNotifications = false } };
        var service = new WindowsNotificationService(tray, storage);

        service.ShowNotification("AutoTyper", "Typing started: Greeting", NotificationType.Information);

        Assert.Empty(tray.BalloonTips);
    }
}
