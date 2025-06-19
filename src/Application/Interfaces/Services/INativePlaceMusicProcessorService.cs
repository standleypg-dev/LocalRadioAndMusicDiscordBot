using System.Diagnostics;

namespace Application.Interfaces.Services;

public interface INativePlaceMusicProcessorService
{
    Task<Process> CreateStream(string audioUrl, CancellationToken cancellationToken);
}