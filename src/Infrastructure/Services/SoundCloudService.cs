using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using SoundCloudExplode;

namespace Infrastructure.Services;

public class SoundCloudService(ILogger<SoundCloudService> logger, SoundCloudClient soundCloudClient): IStreamService
{
    public async Task<string> GetAudioStreamUrlAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            await soundCloudClient.InitializeAsync(cancellationToken);
            var audioStream = await soundCloudClient.Tracks.GetDownloadUrlAsync(url, cancellationToken)
                              ?? throw new InvalidOperationException("No suitable audio format found.");

            return audioStream;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SoundCloudClient failed for: {Url}", url);
            throw;
        }
    }

    public async Task<string> GetVideoTitleAsync(string url, CancellationToken cancellationToken)
    {
        await soundCloudClient.InitializeAsync(cancellationToken);
        var track = await soundCloudClient.Tracks.GetAsync(url, cancellationToken);
        return track?.Title ?? throw new InvalidOperationException($"Could not resolve track title for: {url}");
    }
}
