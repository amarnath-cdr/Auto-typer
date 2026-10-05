namespace AutoTyper.Services.Engine;

/// <summary>
/// Represents the current state of the typing engine.
/// </summary>
public enum TypingState
{
    /// <summary>The engine is idle and ready to begin typing.</summary>
    Ready = 0,

    /// <summary>The engine is actively typing characters.</summary>
    Typing = 1,

    /// <summary>The typing operation was stopped by the user.</summary>
    Stopped = 2,

    /// <summary>The typing operation completed successfully.</summary>
    Completed = 3,

    /// <summary>The typing operation failed due to an error.</summary>
    Error = 4
}
