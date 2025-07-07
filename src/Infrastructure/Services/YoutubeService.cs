using Application.DTOs;
using Application.Interfaces.Services;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoutubeDLSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Infrastructure.Services;

public class YoutubeService(
    IServiceProvider serviceProvider,
    ILogger<YoutubeService> logger,
    IQueueService<SongDto<SocketVoiceChannel>> queueService) : IYoutubeService
{
    public async Task<string> GetAudioStreamUrlAsync(string url)
    {
        try
        {
            var ytdl = new YoutubeDL { YoutubeDLPath = "yt-dlp" };
            var result = await ytdl.RunVideoDataFetch(url);
            result.EnsureSuccess();

            var formats = result.Data.Formats;
            var httpsFormats = formats
                .Where(f => f.ManifestUrl == null && f.Protocol == "https")
                .AsQueryable();

            var bestAudio = httpsFormats
                                .Where(f => f.Resolution == "audio only")
                                .OrderByDescending(f => f.AudioBitrate ?? 0)
                                .FirstOrDefault()
                            ?? httpsFormats
                                .OrderByDescending(f => f.Bitrate ?? 0)
                                .FirstOrDefault();

            if (bestAudio is not null)
            {
                return bestAudio.Url;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "YT-DLP failed for: {Url}", url);
        }

        // Fallback to YoutubeExplode
        try
        {
            using var scope = serviceProvider.CreateScope();
            var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
            var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(url);
            return manifest.GetAudioOnlyStreams().GetWithHighestBitrate().Url;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Both providers failed for: {Url}", url);
            await queueService.SkipSongAsync();
            throw new InvalidOperationException("No suitable audio format found from any provider.");
        }
    }

    public async Task<string> GetVideoTitleAsync(string url)
    {
        using var scope = serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        return (await youtubeClient.Videos.GetAsync(url)).Title;
    }
}