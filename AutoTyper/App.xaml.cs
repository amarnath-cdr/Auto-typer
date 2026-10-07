using System;
using System.Windows;
using System.Windows.Interop;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Services.Clipboard;
using AutoTyper.Services.Engine;
using AutoTyper.Services.Hotkeys;
using AutoTyper.Services.Keyboard;
using AutoTyper.Services.Lifecycle;
using AutoTyper.Services.Notifications;
using AutoTyper.Services.Startup;
using AutoTyper.Services.Tray;
using AutoTyper.ViewModels;
using AutoTyper.Views;

namespace AutoTyper;

public partial class App : System.Windows.Application
{
    private ISingleInstanceService? _singleInstanceService;
    private ITrayIconService? _trayIconService;
    private INotificationService? _notificationService;
    private IStartupService? _startupService;
    private MainViewModel? _mainViewModel;
    private WindowsHotkeyService? _hotkeyService;
    private bool _isExplicitExit;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Enforce single-instance application execution
        _singleInstanceService = new WindowsSingleInstanceService();
        if (!_singleInstanceService.Start())
        {
            _singleInstanceService.SignalExistingInstance();
            Shutdown();
            return;
        }

        // 2. Initialize application services
        var profileStorage = new ProfileStorageService();
        var settingsStorage = new SettingsStorageService();
        var themeService = new ThemeService();
        var keyboardSimulator = new WindowsKeyboardSimulator();
        var delayProvider = new TaskDelayProvider();
        var typingParser = new TypingParser();
        var clipboardService = new WindowsClipboardService();
        var typingEngine = new TypingEngine(keyboardSimulator, delayProvider, typingParser, clipboardService);
        _hotkeyService = new WindowsHotkeyService();
        _startupService = new WindowsStartupService();
        _trayIconService = new WindowsTrayIconService();
        _notificationService = new WindowsNotificationService(_trayIconService, settingsStorage);

        // 3. Compose ViewModel
        _mainViewModel = new MainViewModel(
            profileStorage,
            settingsStorage,
            themeService,
            typingEngine,
            _hotkeyService,
            _notificationService,
            _trayIconService);

        var mainWindow = new MainWindow
        {
            DataContext = _mainViewModel
        };

        MainWindow = mainWindow;

        // 4. Initialize Tray Icon
        _trayIconService.Initialize(
            onShow: () => Dispatcher.Invoke(() => RestoreMainWindow(mainWindow)),
            onStartSelected: () => Dispatcher.Invoke(() => _mainViewModel.StartTypingSelected()),
            onStop: () => Dispatcher.Invoke(() => _mainViewModel.StopTyping()),
            onExit: () => Dispatcher.Invoke(() =>
            {
                _isExplicitExit = true;
                mainWindow.Close();
                Shutdown();
            }));

        // 5. Register Single-Instance activation callback
        _singleInstanceService.RegisterActivationCallback(() =>
        {
            Dispatcher.Invoke(() => RestoreMainWindow(mainWindow));
        });

        // 6. HWND initialization for Global Hotkeys and Window Messages
        mainWindow.SourceInitialized += (s, ev) =>
        {
            var handle = new WindowInteropHelper(mainWindow).Handle;
            if (handle != IntPtr.Zero)
            {
                _hotkeyService.Initialize(handle);
                _mainViewModel.RegisterGlobalHotkeys();

                var source = HwndSource.FromHwnd(handle);
                source?.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                {
                    const int WM_SYSCOMMAND = 0x0112;
                    const int SC_MINIMIZE = 0xF020;

                    if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_MINIMIZE)
                    {
                        var settings = settingsStorage.LoadSettings();
                        if (settings.MinimizeToTray)
                        {
                            mainWindow.Hide();
                            handled = true;
                        }
                    }

                    return IntPtr.Zero;
                });
            }
        };

        // 7. Window close to tray handler
        mainWindow.Closing += (s, ev) =>
        {
            if (!_isExplicitExit)
            {
                var settings = settingsStorage.LoadSettings();
                if (settings.CloseToTray)
                {
                    ev.Cancel = true;
                    mainWindow.Hide();
                }
            }
        };

        mainWindow.Closed += (s, ev) =>
        {
            _mainViewModel?.Dispose();
            _trayIconService?.Dispose();
        };

        // 8. Dialog Callbacks
        _mainViewModel.ShowProfileEditorDialog = (existingProfile) =>
        {
            var editorVm = new ProfileEditorViewModel(existingProfile);
            var editorDialog = new ProfileEditorDialog
            {
                DataContext = editorVm,
                Owner = mainWindow
            };

            editorVm.RequestClose += () => editorDialog.Close();
            editorDialog.ShowDialog();

            if (editorVm.DialogResult)
            {
                return (true, editorVm.BuildProfile());
            }

            return (false, null);
        };

        _mainViewModel.ShowSettingsDialog = () =>
        {
            var settingsVm = new SettingsViewModel(settingsStorage, themeService, _startupService);
            var settingsDialog = new SettingsDialog
            {
                DataContext = settingsVm,
                Owner = mainWindow
            };

            settingsVm.RequestClose += () => settingsDialog.Close();
            settingsDialog.ShowDialog();
        };

        _mainViewModel.ConfirmAction = (message, title) =>
        {
            var res = System.Windows.MessageBox.Show(mainWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return res == MessageBoxResult.Yes;
        };

        mainWindow.Show();
    }

    private static void RestoreMainWindow(MainWindow window)
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Focus();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();
        _trayIconService?.Dispose();
        _singleInstanceService?.Dispose();
        base.OnExit(e);
    }
}
