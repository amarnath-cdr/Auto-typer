using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Utilities;

namespace AutoTyper.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsStorageService _settingsStorage;
    private readonly IThemeService _themeService;

    private AppTheme _theme;
    private bool _autoTyperMasterEnabled;
    private bool _confirmOnDelete;
    private int _defaultDelayMs;
    private string _globalStopHotkey = "Escape";

    public SettingsViewModel(ISettingsStorageService settingsStorage, IThemeService themeService)
    {
        _settingsStorage = settingsStorage ?? throw new ArgumentNullException(nameof(settingsStorage));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        var current = _settingsStorage.LoadSettings();
        _theme = current.Theme;
        _autoTyperMasterEnabled = current.AutoTyperMasterEnabled;
        _confirmOnDelete = current.ConfirmOnDelete;
        _defaultDelayMs = current.DefaultDelayMs;
        _globalStopHotkey = string.IsNullOrWhiteSpace(current.GlobalStopHotkey) ? "Escape" : current.GlobalStopHotkey;

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

    public string ConfigFolderPath => PathConstants.DataDirectory;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }

    private void Save()
    {
        var settings = new AppSettings
        {
            Theme = Theme,
            AutoTyperMasterEnabled = AutoTyperMasterEnabled,
            ConfirmOnDelete = ConfirmOnDelete,
            DefaultDelayMs = DefaultDelayMs,
            GlobalStopHotkey = GlobalStopHotkey
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
