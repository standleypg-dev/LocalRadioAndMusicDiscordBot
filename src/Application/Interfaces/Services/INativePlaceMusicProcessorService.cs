using System.Diagnostics;

namespace Application.Interfaces.Services;

public interface INativePlaceMusicProcessorService
{
    Task<Process> CreateStreamAsync(string audioUrl, CancellationToken cancellationToken);
    event Func<Task>? OnExitProcess;
    event Func<Task>? OnProcessStart;
}