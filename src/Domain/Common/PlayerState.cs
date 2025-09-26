namespace Domain.Common;

public class PlayerState
{
    /// <summary>
    /// Cancellation token source for stop event.
    /// </summary>
    public CancellationTokenSource? StopCts { get; set; }
    /// <summary>
    /// Cancellation token source for skip event.
    /// </summary>
    public CancellationTokenSource? SkipCts { get; set; }

    public Func<Task>? DisconnectAsyncCallback { get; set; }
    public bool IsPlaying { get; set; }
}