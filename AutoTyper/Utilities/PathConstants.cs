using System;
using System.IO;

namespace AutoTyper.Utilities;

public static class PathConstants
{
    private static string? _customDataDirectory;

    public static string DefaultDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoTyper");

    public static string DataDirectory
    {
        get => _customDataDirectory ?? DefaultDataDirectory;
        set => _customDataDirectory = value;
    }

    public static string ProfilesFilePath => Path.Combine(DataDirectory, "profiles.json");

    public static string SettingsFilePath => Path.Combine(DataDirectory, "settings.json");

    public static void ResetDataDirectory()
    {
        _customDataDirectory = null;
    }

    public static void EnsureDataDirectoryExists()
    {
        if (!Directory.Exists(DataDirectory))
        {
            Directory.CreateDirectory(DataDirectory);
        }
    }
}
