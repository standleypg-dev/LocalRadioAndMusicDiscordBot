using Application.DTOs;

namespace Application.Interfaces.Services;

public interface INetCordAudioPlayerService
{
    /// <summary>
    /// Plays a single request to completion. Joins the voice channel when not connected.
    /// Returns when the track finishes, fails, or is cancelled.
    /// </summary>
    /// <param name="request">The track to play.</param>
    /// <param name="attempt">1-based retry attempt for this request, used to vary stream-source provider order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TrackPlayResult> PlayTrackAsync(PlayRequest request, int attempt, CancellationToken cancellationToken);

    /// <summary>
    /// Leaves the voice channel and releases the voice client.
    /// </summary>
    Task DisconnectAsync();
}
