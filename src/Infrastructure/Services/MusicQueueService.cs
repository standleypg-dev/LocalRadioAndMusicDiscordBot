using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

public class MusicQueueService(ILogger<MusicQueueService> logger) : IMusicQueueService
{
    private readonly Queue<PlayRequest<StringMenuInteractionContext>> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly Lock _lock = new();

    public void Enqueue<T>(PlayRequest<T> request)
    {
        if (request is not PlayRequest<StringMenuInteractionContext> playRequest)
        {
            throw new ArgumentException($"Invalid request type. Expected {typeof(PlayRequest<StringMenuInteractionContext>)}", nameof(request));
        }
        lock (_lock)
        {
            _queue.Enqueue(playRequest);
            _signal.Release();
        }
    }

    public  PlayRequest<T>? Peek<T>()
    {

        lock (_lock)
        {
            if (!_queue.TryPeek(out var result) || result is not PlayRequest<T> currentRequest)
            {
                logger.LogWarning("Peeked request is not of the expected type {ExpectedType}", typeof(PlayRequest<T>));
                return null;
            }
            
            return _queue.Count > 0 ? currentRequest : null;
        }
    }

    public async void DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
                _queue.Dequeue();
            }
            else
            {
                logger.LogWarning("Attempted to dequeue from an empty queue.");
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }
}