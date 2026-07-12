using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IMusicQueueService
{
    void Enqueue<T>(PlayRequest<T> request);

    /// <summary>
    /// Waits until a request is available and removes it from the pending queue.
    /// Intended to be consumed by a single background consumer.
    /// </summary>
    ValueTask<PlayRequest<T>> DequeueAsync<T>(CancellationToken cancellationToken);

    /// <summary>
    /// Number of pending requests (excludes the currently playing one).
    /// </summary>
    int Count { get; }

    /// <summary>
    /// The request currently being played, if any. Owned by the player background service.
    /// </summary>
    PlayRequest? NowPlaying { get; }

    void SetNowPlaying(PlayRequest? request);

    /// <summary>
    /// Snapshot of the queue: the currently playing request first (if any), then pending ones.
    /// </summary>
    PlayRequest[] GetAllRequests();

    /// <summary>
    /// Re-queues the currently playing request at the front so it plays again.
    /// </summary>
    void Rewind();

    void Clear();
}
