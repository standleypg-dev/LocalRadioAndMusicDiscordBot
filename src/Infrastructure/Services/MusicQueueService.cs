using System.Threading.Channels;
using Application.DTOs;
using Application.Interfaces.Services;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

/// <summary>
/// In-memory music queue following the channel-backed queue service pattern:
/// a lock-protected list is the source of truth and an unbounded channel is the
/// async signal consumed by the single player background service.
/// Every enqueue writes exactly one signal; a dequeue consumes one signal per
/// returned item, so stale signals left behind by <see cref="Clear"/> are
/// absorbed harmlessly (the consumer finds the list empty and waits again).
/// </summary>
public class MusicQueueService : IMusicQueueService
{
    private readonly Channel<bool> _signal = Channel.CreateUnbounded<bool>();
    private readonly List<PlayRequest<StringMenuInteractionContext>> _items = [];
    private readonly Lock _lock = new();
    private PlayRequest? _nowPlaying;

    public void Enqueue<T>(PlayRequest<T> request)
    {
        if (request is not PlayRequest<StringMenuInteractionContext> playRequest)
        {
            throw new ArgumentException(
                $"Invalid request type. Expected {typeof(PlayRequest<StringMenuInteractionContext>)}",
                nameof(request));
        }

        lock (_lock)
        {
            _items.Add(playRequest);
        }

        _signal.Writer.TryWrite(true);
    }

    public async ValueTask<PlayRequest<T>> DequeueAsync<T>(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _signal.Reader.ReadAsync(cancellationToken);

            lock (_lock)
            {
                if (_items.Count > 0)
                {
                    var item = _items[0];
                    _items.RemoveAt(0);
                    return item as PlayRequest<T>
                           ?? throw new InvalidOperationException(
                               $"Queued request is not of the expected type {typeof(PlayRequest<T>)}.");
                }
            }

            // Stale signal (the queue was cleared since the signal was written); wait for the next enqueue.
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _items.Count;
            }
        }
    }

    public PlayRequest? NowPlaying
    {
        get
        {
            lock (_lock)
            {
                return _nowPlaying;
            }
        }
    }

    public void SetNowPlaying(PlayRequest? request)
    {
        lock (_lock)
        {
            _nowPlaying = request;
        }
    }

    public PlayRequest[] GetAllRequests()
    {
        lock (_lock)
        {
            return _nowPlaying is null
                ? _items.ToArray<PlayRequest>()
                : [_nowPlaying, .. _items];
        }
    }

    public void Rewind()
    {
        lock (_lock)
        {
            if (_nowPlaying is not PlayRequest<StringMenuInteractionContext> current)
            {
                return;
            }

            current.RetryCount = 0;
            _items.Insert(0, current);
        }

        _signal.Writer.TryWrite(true);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _items.Clear();
        }
    }
}
