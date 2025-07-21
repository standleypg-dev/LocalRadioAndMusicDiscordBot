using System.Diagnostics;
using Application.DTOs;
using Application.Interfaces.Services;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoutubeDLSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Infrastructure.Services;

public class YoutubeService: IYoutubeService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<YoutubeService> _logger;
    private readonly IQueueService<SongDto<SocketVoiceChannel>> _queueService;
    private readonly List<Func<string, Task<(bool Success, string? Url)>>> _providerStrategy;

    public YoutubeService(
        IServiceProvider serviceProvider,
        ILogger<YoutubeService> logger,
        IQueueService<SongDto<SocketVoiceChannel>> queueService)
    {
        _serviceProvider = serviceProvider;
        _queueService = queueService;
        _logger = logger;

        _providerStrategy =
        [
            TryGetWithYtDlpAsync,
            TryGetWithYoutubeExplodeAsync,
        ];
    }

    public async Task<string> GetAudioStreamUrlAsync(string url)
    {
        foreach (var strategy in _providerStrategy)
        {
            var result = await ExecuteWithTimeout(strategy, url, TimeSpan.FromSeconds(15));
            if (result.Success)
            {
                _logger.LogInformation("Successfully obtained stream URL using {Provider}", 
                    strategy.Method.Name.Replace("TryGetWith", "").Replace("Async", ""));
                return result.Url!;
            }
        }

        _logger.LogError("All audio stream providers failed for: {Url}", url);
        await _queueService.SkipSongAsync();
        throw new InvalidOperationException("No suitable audio format found from any provider.");
    }

    private async Task<(bool Success, string? Url)> ExecuteWithTimeout(
        Func<string, Task<(bool, string?)>> func, 
        string url,
        TimeSpan timeout)
    {
        try
        {
            var task = func(url);
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout));
            
            if (completedTask == task) 
                return await task;
            
            _logger.LogWarning("Provider {Provider} timed out after {Timeout} seconds", 
                func.Method.Name.Replace("TryGetWith", "").Replace("Async", ""), 
                timeout.TotalSeconds);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {Provider} failed", 
                func.Method.Name.Replace("TryGetWith", "").Replace("Async", ""));
            return (false, null);
        }
    }

    private async Task<(bool Success, string? Url)> TryGetWithYtDlpAsync(string url)
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
                                .MaxBy(f => f.AudioBitrate ?? 0)
                            ?? httpsFormats
                                .MaxBy(f => f.Bitrate ?? 0);
            
            if (bestAudio?.Url == null)
            {
                _logger.LogWarning("YT-DLP found no suitable audio format for: {Url}", url);
                return (false, null);
            }

            // Verify URL is accessible
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(bestAudio.Url);
            response.EnsureSuccessStatusCode();

            return (true, bestAudio.Url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YT-DLP failed for: {Url}", url);
            return (false, null);
        }
    }

    private async Task<(bool Success, string? Url)> TryGetWithYoutubeExplodeAsync(string url)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
            var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(url);
            var audioStream = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            return (true, audioStream.Url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YoutubeExplode failed for: {Url}", url);
            return (false, null);
        }
    }
    
    public async Task<string> GetVideoTitleAsync(string url)
    {
        using var scope = _serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        return (await youtubeClient.Videos.GetAsync(url)).Title;
    }
}