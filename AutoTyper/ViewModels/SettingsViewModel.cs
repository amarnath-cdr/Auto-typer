using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Services.Startup;
using AutoTyper.Utilities;

namespace AutoTyper.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsStorageService _settingsStorage;
    private readonly IThemeService _themeService;
    private readonly IStartupService? _startupService;

    private AppTheme _theme;
    private bool _autoTyperMasterEnabled;
    private bool _confirmOnDelete;
    private int _defaultDelayMs;
    private string _globalStopHotkey = "Escape";
    private bool _minimizeToTray;
    private bool _closeToTray;
    private bool _startWithWindows;
    private bool _showNotifications;

    public SettingsViewModel(
        ISettingsStorageService settingsStorage,
        IThemeService themeService,
        IStartupService? startupService = null)
    {
        _settingsStorage = settingsStorage ?? throw new ArgumentNullException(nameof(settingsStorage));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _startupService = startupService;

        var current = _settingsStorage.LoadSettings();
        _theme = current.Theme;
        _autoTyperMasterEnabled = current.AutoTyperMasterEnabled;
        _confirmOnDelete = current.ConfirmOnDelete;
        _defaultDelayMs = current.DefaultDelayMs;
        _globalStopHotkey = string.IsNullOrWhiteSpace(current.GlobalStopHotkey) ? "Escape" : current.GlobalStopHotkey;
        _minimizeToTray = current.MinimizeToTray;
        _closeToTray = current.CloseToTray;
        _startWithWindows = _startupService != null ? _startupService.IsStartWithWindowsEnabled() : current.StartWithWindows;
        _showNotifications = current.ShowNotifications;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder);
    }

    public event Action? RequestClose;
    public bool DialogResult { get; private set; }

    public AppTheme Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                // Live preview theme
                _themeService.ApplyTheme(value);
            }
        }
    }

    public bool AutoTyperMasterEnabled
    {
        get => _autoTyperMasterEnabled;
        set => SetProperty(ref _autoTyperMasterEnabled, value);
    }

    public bool ConfirmOnDelete
    {
        get => _confirmOnDelete;
        set => SetProperty(ref _confirmOnDelete, value);
    }

    public int DefaultDelayMs
    {
        get => _defaultDelayMs;
        set => SetProperty(ref _defaultDelayMs, value);
    }

    public string GlobalStopHotkey
    {
        get => _globalStopHotkey;
        set => SetProperty(ref _globalStopHotkey, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        set => SetProperty(ref _closeToTray, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    public bool ShowNotifications
    {
        get => _showNotifications;
        set => SetProperty(ref _showNotifications, value);
    }

    public string ConfigFolderPath => PathConstants.DataDirectory;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }

    private void Save()
    {
        // Update Windows startup setting
        _startupService?.SetStartWithWindows(StartWithWindows);

        var settings = new AppSettings
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

        _settingsStorage.SaveSettings(settings);
        _themeService.ApplyTheme(settings.Theme);

        DialogResult = true;
        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        // Revert theme preview
        var saved = _settingsStorage.LoadSettings();
        _themeService.ApplyTheme(saved.Theme);

        DialogResult = false;
        RequestClose?.Invoke();
    }

    private void OpenConfigFolder()
    {
        try
        {
            PathConstants.EnsureDataDirectoryExists();
            Process.Start(new ProcessStartInfo
            {
                FileName = PathConstants.DataDirectory,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore if shell cannot open directory
        }
    }
}
