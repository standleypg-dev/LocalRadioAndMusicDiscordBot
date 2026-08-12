using Application.DTOs;

namespace Application.Interfaces.Services;

public interface INetCordAudioPlayerService
{
    /// <summary>
    /// Plays a single request to completion. Joins the voice channel when not connected.
    /// Returns when the track finishes, fails, or is cancelled.
    /// </summary>
    Task<TrackPlayResult> PlayTrackAsync(PlayRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Leaves the voice channel and releases the voice client.
    /// </summary>
    Task DisconnectAsync();
}
