namespace AutoTyper.Services.Clipboard;

/// <summary>
/// Abstraction for accessing and modifying the system clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Retrieves the current plain text from the clipboard.
    /// </summary>
    string? GetText();

    /// <summary>
    /// Sets plain text onto the clipboard.
    /// </summary>
    void SetText(string text);

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    void Clear();
}
