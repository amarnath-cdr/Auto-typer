using System;
using System.Drawing;
using System.Windows.Forms;
using AutoTyper.Services.Notifications;

namespace AutoTyper.Services.Tray;

/// <summary>
/// Windows system tray icon implementation using <see cref="NotifyIcon"/>.
/// </summary>
public class WindowsTrayIconService : ITrayIconService
{
    private NotifyIcon? _notifyIcon;
    private ToolStripMenuItem? _startMenuItem;
    private ToolStripMenuItem? _stopMenuItem;
    private bool _disposed;

    /// <inheritdoc />
    public void Initialize(Action onShow, Action onStartSelected, Action onStop, Action onExit)
    {
        ArgumentNullException.ThrowIfNull(onShow);
        ArgumentNullException.ThrowIfNull(onStartSelected);
        ArgumentNullException.ThrowIfNull(onStop);
        ArgumentNullException.ThrowIfNull(onExit);

        try
        {
            var contextMenu = new ContextMenuStrip();

            var showMenuItem = new ToolStripMenuItem("Show AutoTyper", null, (s, e) => onShow())
            {
                Font = new Font(contextMenu.Font, FontStyle.Bold)
            };

            _startMenuItem = new ToolStripMenuItem("Start Selected Profile", null, (s, e) => onStartSelected())
            {
                Enabled = false
            };

            _stopMenuItem = new ToolStripMenuItem("Stop Typing", null, (s, e) => onStop())
            {
                Enabled = false
            };

            var exitMenuItem = new ToolStripMenuItem("Exit", null, (s, e) => onExit());

            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(_startMenuItem);
            contextMenu.Items.Add(_stopMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitMenuItem);

            Icon appIcon = SystemIcons.Application;
            try
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                {
                    var extracted = Icon.ExtractAssociatedIcon(processPath);
                    if (extracted != null)
                        appIcon = extracted;
                }
            }
            catch
            {
                // Fallback to default system application icon
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "AutoTyper",
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => onShow();
        }
        catch
        {
            // If tray initialization fails, allow the app to continue without tray
            _notifyIcon = null;
        }
    }

    /// <inheritdoc />
    public void UpdateMenuState(bool canStartSelected, bool isTyping)
    {
        if (_notifyIcon == null || _disposed)
            return;

        try
        {
            if (_startMenuItem != null)
            {
                _startMenuItem.Enabled = canStartSelected && !isTyping;
            }

            if (_stopMenuItem != null)
            {
                _stopMenuItem.Enabled = isTyping;
            }
        }
        catch
        {
            // Ignore UI thread / disposition race
        }
    }

    /// <inheritdoc />
    public void ShowBalloonTip(string title, string message, NotificationType type, int timeoutMs = 3000)
    {
        if (_notifyIcon == null || !_notifyIcon.Visible || _disposed)
            return;

        try
        {
            var icon = type switch
            {
                NotificationType.Warning => ToolTipIcon.Warning,
                NotificationType.Error => ToolTipIcon.Error,
                _ => ToolTipIcon.Info
            };

            _notifyIcon.ShowBalloonTip(timeoutMs, title, message, icon);
        }
        catch
        {
            // Ignore notification failure
        }
    }

    /// <inheritdoc />
    public void SetVisible(bool visible)
    {
        if (_notifyIcon != null && !_disposed)
        {
            try
            {
                _notifyIcon.Visible = visible;
            }
            catch
            {
                // Ignore
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_notifyIcon != null)
            {
                try
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.ContextMenuStrip?.Dispose();
                    _notifyIcon.Dispose();
                }
                catch
                {
                    // Ignore disposal errors on shutdown
                }
                finally
                {
                    _notifyIcon = null;
                }
            }
            _disposed = true;
        }
    }
}
