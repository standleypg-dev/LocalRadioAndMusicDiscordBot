using System.Diagnostics;

namespace Application.Interfaces.Services;

public interface INativePlaceMusicProcessorService
{
    Task<Process> CreateStreamAsync(string audioUrl, CancellationToken cancellationToken);
    event Func<Task>? OnPlaySongCompleted;
    event Func<Task>? OnProcessStart;
    event Func<Task>? OnForbiddenUrlRequest;
}