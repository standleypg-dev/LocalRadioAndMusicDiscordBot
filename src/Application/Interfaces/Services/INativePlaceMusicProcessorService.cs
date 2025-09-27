using System.Diagnostics;

namespace Application.Interfaces.Services;

public interface INativePlaceMusicProcessorService
{
    event Func<Task>? OnExitProcess;
    Task<Process> CreateStreamAsync(string audioUrl, CancellationToken cancellationToken);
}