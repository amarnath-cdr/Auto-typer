using System.Windows;
using AutoTyper.Models;
using AutoTyper.Services;
using AutoTyper.ViewModels;
using AutoTyper.Views;

namespace AutoTyper;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var profileStorage = new ProfileStorageService();
        var settingsStorage = new SettingsStorageService();
        var themeService = new ThemeService();

        var mainVm = new MainViewModel(profileStorage, settingsStorage, themeService);

        var mainWindow = new MainWindow
        {
            DataContext = mainVm
        };

        mainVm.ShowProfileEditorDialog = (existingProfile) =>
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

        mainVm.ShowSettingsDialog = () =>
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

        mainVm.ConfirmAction = (message, title) =>
        {
            var res = MessageBox.Show(mainWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return res == MessageBoxResult.Yes;
        };

        mainWindow.Show();
    }
}
