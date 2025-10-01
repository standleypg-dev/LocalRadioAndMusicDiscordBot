using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IMusicQueueService
{
    void Enqueue<T>(PlayRequest<T> request);
    PlayRequest<T>? Peek<T>();
    void DequeueAsync(CancellationToken cancellationToken);
    int Count { get; }
    PlayRequest[] GetAllRequests();
    void Clear();
}