using System;

namespace AutoTyper.Models;

public class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;

    public bool AutoTyperMasterEnabled { get; set; } = true;

    public bool ConfirmOnDelete { get; set; } = true;

    public int DefaultDelayMs { get; set; } = 20;

    public string GlobalStopHotkey { get; set; } = "Escape";

    public bool MinimizeToTray { get; set; } = true;

    public bool CloseToTray { get; set; } = true;

    public bool StartWithWindows { get; set; } = false;

    public bool ShowNotifications { get; set; } = true;

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Theme = Theme,
            AutoTyperMasterEnabled = AutoTyperMasterEnabled,
            ConfirmOnDelete = ConfirmOnDelete,
            DefaultDelayMs = DefaultDelayMs,
            GlobalStopHotkey = GlobalStopHotkey,
            MinimizeToTray = MinimizeToTray,
            CloseToTray = CloseToTray,
            StartWithWindows = StartWithWindows,
            ShowNotifications = ShowNotifications
        };
    }

    public void CopyFrom(AppSettings other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Theme = other.Theme;
        AutoTyperMasterEnabled = other.AutoTyperMasterEnabled;
        ConfirmOnDelete = other.ConfirmOnDelete;
        DefaultDelayMs = other.DefaultDelayMs;
        GlobalStopHotkey = other.GlobalStopHotkey;
        MinimizeToTray = other.MinimizeToTray;
        CloseToTray = other.CloseToTray;
        StartWithWindows = other.StartWithWindows;
        ShowNotifications = other.ShowNotifications;
    }
}
