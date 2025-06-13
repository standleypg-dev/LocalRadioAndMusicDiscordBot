using radio_discord_bot.Services.Interfaces;
using YoutubeDLSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace radio_discord_bot.Services;

public class YoutubeService(IServiceProvider serviceProvider, ILogger<YoutubeService> logger) : IYoutubeService
{
    public async Task<string> GetAudioStreamUrlAsync(string url)
    {
        var ytdl = new YoutubeDL
        {
            YoutubeDLPath = "yt-dlp"
        };

        var result = await ytdl.RunVideoDataFetch(url);

        if (!result.Success)
        {
            logger.LogError("Failed to fetch video info: {ErrorOutput}", string.Join(Environment.NewLine, result.ErrorOutput));
        }
        
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
        
        if (bestAudio == null)
        {
            logger.LogError("No suitable audio format found for URL: {Url}", url);
            throw new InvalidOperationException("No suitable audio format found.");
        }
        
        return bestAudio.Url;
    }

    public async Task<string> GetVideoTitleAsync(string url)
    {
        using var scope = serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        return (await youtubeClient.Videos.GetAsync(url)).Title;
    }
}