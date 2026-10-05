using System;

namespace AutoTyper.Models;

public class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;

    public bool AutoTyperMasterEnabled { get; set; } = true;

    public bool ConfirmOnDelete { get; set; } = true;

    public int DefaultDelayMs { get; set; } = 20;

    public string GlobalStopHotkey { get; set; } = "Escape";

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Theme = Theme,
            AutoTyperMasterEnabled = AutoTyperMasterEnabled,
            ConfirmOnDelete = ConfirmOnDelete,
            DefaultDelayMs = DefaultDelayMs,
            GlobalStopHotkey = GlobalStopHotkey
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
    }
}
