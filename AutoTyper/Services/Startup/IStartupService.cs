namespace AutoTyper.Services.Startup;

/// <summary>
/// Abstraction for managing application startup on user sign-in.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// Checks whether the application is configured to run at Windows sign-in.
    /// </summary>
    bool IsStartWithWindowsEnabled();

    /// <summary>
    /// Enables or disables application startup at Windows sign-in.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    /// <returns>True if the operation succeeded; false otherwise.</returns>
    bool SetStartWithWindows(bool enabled);
}
