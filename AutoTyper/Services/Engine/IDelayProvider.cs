using System.Threading;
using System.Threading.Tasks;

namespace AutoTyper.Services.Engine;

/// <summary>
/// Abstraction for delay between keystrokes, enabling deterministic testing.
/// </summary>
public interface IDelayProvider
{
    /// <summary>
    /// Waits for the specified number of milliseconds.
    /// </summary>
    Task DelayAsync(int milliseconds, CancellationToken cancellationToken);
}

/// <summary>
/// Production delay provider using <see cref="Task.Delay(int, CancellationToken)"/>.
/// </summary>
public class TaskDelayProvider : IDelayProvider
{
    public async Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
    {
        if (milliseconds > 0)
        {
            await Task.Delay(milliseconds, cancellationToken);
        }
    }
}
