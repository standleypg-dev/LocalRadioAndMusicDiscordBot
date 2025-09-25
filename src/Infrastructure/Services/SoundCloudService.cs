using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using SoundCloudExplode;
using SoundCloudExplode.Common;

namespace Infrastructure.Services;

public class SoundCloudService(ILogger<SoundCloudService> logger, SoundCloudClient soundCloudClient): IStreamService
{
    public async Task<string> GetAudioStreamUrlAsync(string url)
    {
        try
        {
            await soundCloudClient.InitializeAsync();
            var audioStream = await soundCloudClient.Tracks.GetDownloadUrlAsync(url) ?? throw new InvalidOperationException("No suitable audio format found.");

            return audioStream;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SoundCloudClient failed for: {Url}", url);
            return null!;
        }
    }

    public Task<string> GetVideoTitleAsync(string url)
    {
        throw new NotImplementedException();
    }
}