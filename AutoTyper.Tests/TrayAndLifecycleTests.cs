using System;
using System.Threading;
using AutoTyper.Models;
using AutoTyper.Services.Lifecycle;
using AutoTyper.Services.Notifications;
using AutoTyper.Services.Tray;
using Xunit;

namespace AutoTyper.Tests;

public class TrayAndLifecycleTests
{
    [Fact]
    public void WindowsTrayIconService_HandlesMenuStateTransitions()
    {
        using var trayService = new WindowsTrayIconService();
        int callbackCount = 0;

        trayService.Initialize(
            () => callbackCount++,
            () => callbackCount++,
            () => callbackCount++,
            () => callbackCount++);

        // Update state when idle with selection
        trayService.UpdateMenuState(canStartSelected: true, isTyping: false);

        // Update state when typing
        trayService.UpdateMenuState(canStartSelected: false, isTyping: true);

        // Update state when stopped
        trayService.UpdateMenuState(canStartSelected: true, isTyping: false);

        trayService.SetVisible(true);
        trayService.ShowBalloonTip("Test", "Message", NotificationType.Information);

        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void WindowsSingleInstanceService_FirstInstance_AcquiresSuccessfully()
    {
        var uniqueId = $"Test_Instance_{Guid.NewGuid():N}";
        using var instance = new WindowsSingleInstanceService(uniqueId);

        var acquired = instance.Start();

        Assert.True(acquired);
    }

    [Fact]
    public void WindowsSingleInstanceService_SecondInstance_IsRejectedAndSignalsFirst()
    {
        var uniqueId = $"Test_Instance_Pair_{Guid.NewGuid():N}";
        using var firstInstance = new WindowsSingleInstanceService(uniqueId);
        using var secondInstance = new WindowsSingleInstanceService(uniqueId);

        bool activated = false;
        using var activationEvent = new AutoResetEvent(false);

        firstInstance.RegisterActivationCallback(() =>
        {
            activated = true;
            activationEvent.Set();
        });

        var firstAcquired = firstInstance.Start();
        Assert.True(firstAcquired);

        var secondAcquired = secondInstance.Start();
        Assert.False(secondAcquired);

        secondInstance.SignalExistingInstance();

        var signaled = activationEvent.WaitOne(2000);
        Assert.True(signaled);
        Assert.True(activated);
    }
}
