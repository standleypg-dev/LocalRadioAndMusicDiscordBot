using System.Diagnostics;

namespace Application.Interfaces.Services;

public interface INativePlaceMusicProcessorService
{
    /// <summary>
    /// Stops any previously running process and starts a new ffmpeg process decoding the given URL to PCM on stdout.
    /// </summary>
    Task<Process> CreateStreamAsync(string audioUrl, CancellationToken cancellationToken);

    /// <summary>
    /// Gracefully terminates the current ffmpeg process (stdin "q", then kill after a grace period).
    /// </summary>
    Task StopCurrentProcessAsync();
}
