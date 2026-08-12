namespace Application.DTOs;

public enum TrackPlayResult
{
    /// <summary>The track played to the end (ffmpeg exit code 0).</summary>
    Completed,

    /// <summary>Playback failed (non-zero exit code or source resolution failure); candidate for retry.</summary>
    Failed,

    /// <summary>Playback was cancelled via the track cancellation token (skip or stop).</summary>
    Skipped,

    /// <summary>The requesting user is not in a voice channel; the track is dropped.</summary>
    NotInVoiceChannel
}
