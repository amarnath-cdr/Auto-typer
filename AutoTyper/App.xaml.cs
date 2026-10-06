using System;
using System.Windows;
using System.Windows.Interop;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.Services.Clipboard;
using AutoTyper.Services.Engine;
using AutoTyper.Services.Hotkeys;
using AutoTyper.Services.Keyboard;
using AutoTyper.ViewModels;
using AutoTyper.Views;

namespace AutoTyper;

public partial class App : Application
{
    private MainViewModel? _mainViewModel;
    private WindowsHotkeyService? _hotkeyService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var profileStorage = new ProfileStorageService();
        var settingsStorage = new SettingsStorageService();
        var themeService = new ThemeService();
        var keyboardSimulator = new WindowsKeyboardSimulator();
        var delayProvider = new TaskDelayProvider();
        var typingParser = new TypingParser();
        var clipboardService = new WindowsClipboardService();
        var typingEngine = new TypingEngine(keyboardSimulator, delayProvider, typingParser, clipboardService);
        _hotkeyService = new WindowsHotkeyService();

        _mainViewModel = new MainViewModel(
            profileStorage,
            settingsStorage,
            themeService,
            typingEngine,
            _hotkeyService);

        var mainWindow = new MainWindow
        {
            DataContext = _mainViewModel
        };

        MainWindow = mainWindow;

        mainWindow.SourceInitialized += (s, ev) =>
        {
            var handle = new WindowInteropHelper(mainWindow).Handle;
            if (handle != IntPtr.Zero)
            {
                _hotkeyService.Initialize(handle);
                _mainViewModel.RegisterGlobalHotkeys();
            }
        };

        mainWindow.Closed += (s, ev) =>
        {
            _mainViewModel.Dispose();
        };

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
            var settingsVm = new SettingsViewModel(settingsStorage, themeService);
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
            var res = MessageBox.Show(mainWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return res == MessageBoxResult.Yes;
        };

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();
        base.OnExit(e);
    }
}
