namespace Application.Interfaces.Services;

/// <summary>
/// Control surface for the music player background service.
/// </summary>
public interface IMusicPlayerController
{
    /// <summary>
    /// Cancels the currently playing track, if any. The player advances to the next queued track.
    /// </summary>
    void Skip();

    /// <summary>
    /// Clears the pending queue and cancels the currently playing track.
    /// The player disconnects from the voice channel once idle.
    /// </summary>
    void Stop();
}
