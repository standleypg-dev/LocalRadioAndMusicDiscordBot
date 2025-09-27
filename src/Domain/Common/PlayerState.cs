namespace Domain.Common;

public class PlayerState<TVoiceClient>
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
    public PlayerAction CurrentAction { get; set; } = PlayerAction.Stop;
    
    public TVoiceClient? CurrentVoiceClient { get; set; }
}

public enum PlayerAction
{
    Play,
    Stop,
    Skip
}