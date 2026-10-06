using System;
using System.Linq;
using System.Windows;
using AutoTyper.Models;
using Microsoft.Win32;

namespace AutoTyper.Services;

public class ThemeService : IThemeService
{
    private const string DarkThemeUri = "Resources/Themes/DarkTheme.xaml";
    private const string LightThemeUri = "Resources/Themes/LightTheme.xaml";

    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    public void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;

        bool useDark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsWindowsInDarkMode()
        };

        var targetUri = useDark ? DarkThemeUri : LightThemeUri;

        if (System.Windows.Application.Current == null)
            return;

        var mergedDicts = System.Windows.Application.Current.Resources.MergedDictionaries;
        var existingThemeDict = mergedDicts.FirstOrDefault(d =>
            d.Source != null && (d.Source.OriginalString.Contains("DarkTheme.xaml") || d.Source.OriginalString.Contains("LightTheme.xaml")));

        var newThemeDict = new ResourceDictionary
        {
            Source = new Uri(targetUri, UriKind.Relative)
        };

        if (existingThemeDict != null)
        {
            var index = mergedDicts.IndexOf(existingThemeDict);
            mergedDicts[index] = newThemeDict;
        }
        else
        {
            mergedDicts.Insert(0, newThemeDict);
        }
    }

    private static bool IsWindowsInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int intVal)
            {
                return intVal == 0;
            }
        }
        catch
        {
            // Ignore and fallback to light
        }

        return false;
    }
}
