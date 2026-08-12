using Application.DTOs;

namespace Application.Interfaces.Services;

/// <summary>
/// Guild-scoped facade over the per-guild music players. Commands and interactions
/// address a specific guild's queue and playback through this interface; players are
/// created lazily on the first enqueue for a guild.
/// </summary>
public interface IGuildMusicService
{
    void Enqueue<T>(ulong guildId, PlayRequest<T> request);

    /// <summary>
    /// The request currently being played in the guild, or null when the guild has no
    /// player or nothing is playing.
    /// </summary>
    PlayRequest? GetNowPlaying(ulong guildId);

    /// <summary>
    /// Snapshot of the guild's queue: the currently playing request first (if any),
    /// then pending ones. Empty when the guild has no player.
    /// </summary>
    PlayRequest[] GetAllRequests(ulong guildId);

    /// <summary>
    /// Number of pending requests in the guild (excludes the currently playing one).
    /// </summary>
    int GetQueueCount(ulong guildId);

    /// <summary>
    /// Re-queues the guild's currently playing request at the front so it plays again.
    /// </summary>
    void Rewind(ulong guildId);

    /// <summary>
    /// Cancels the guild's current track. No-op when the guild has no player.
    /// </summary>
    void Skip(ulong guildId);

    /// <summary>
    /// Clears the guild's queue and cancels its current track. No-op when the guild
    /// has no player.
    /// </summary>
    void Stop(ulong guildId);
}
