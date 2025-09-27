using System.Diagnostics;

namespace Application.Interfaces.Services;

public interface INativePlaceMusicProcessorService
{
    Task<Process> CreateStreamAsync(string audioUrl, Func<Task> disconnectVoiceClient, CancellationToken cancellationToken);
}