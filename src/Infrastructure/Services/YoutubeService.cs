using Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Infrastructure.Services;

public class YoutubeService: IStreamService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<YoutubeService> _logger;
    private readonly List<Func<string, CancellationToken, Task<(bool Success, string? Url)>>> _providerStrategy;
    private readonly YoutubeClient _youtubeClient;

    // YouTube blocks/throttles requests from datacenter IPs (common for self-hosted bots),
    // often surfacing as false "video unavailable" errors from both yt-dlp and YoutubeExplode.
    // bgutil-ytdlp-pot-provider (deployment/docker-compose.yml) mints proof-of-origin tokens so
    // yt-dlp's requests look like a real browser session; see https://github.com/Brainicism/bgutil-ytdlp-pot-provider.
    private readonly string _potProviderExtractorArg;

    // bgutil only mints web-flavored PO tokens (web/mweb/web_safari). yt-dlp still lists formats
    // from other clients (e.g. android_vr) as available whenever any PO token provider is
    // registered, but android_vr's playback URLs need a separate DroidGuard-based token bgutil
    // can't supply, so picking one 403s. Pin player_client so only bgutil-covered clients are used.
    private const string PlayerClientExtractorArg = "youtube:player_client=web,mweb";

    public YoutubeService(
        IServiceProvider serviceProvider,
        ILogger<YoutubeService> logger,
        YoutubeClient youtubeClient,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _youtubeClient = youtubeClient;

        var potProviderBaseUrl = configuration["YtDlp:PotProviderBaseUrl"] ?? "http://bgutil-provider:4416";
        _potProviderExtractorArg = $"youtubepot-bgutilhttp:base_url={potProviderBaseUrl}";

        _providerStrategy =
        [
            TryGetWithYtDlpAsync,
            TryGetWithYoutubeExplodeAsync,
        ];
    }

    public async Task<string> GetAudioStreamUrlAsync(string url, int attempt, CancellationToken cancellationToken)
    {
        // yt-dlp can report a successful fetch but hand back a URL that still 403s once FFmpeg
        // opens it (e.g. YouTube binding the GVS PO token to a specific video ID) - a failure this
        // class can't see, since it only surfaces later in FfmpegProcessService. This class is
        // re-created per attempt (scoped DI, fresh scope per PlayTrackAsync call), so it has no
        // memory of prior attempts - GuildPlayer's retry loop passes the attempt number in instead.
        // Alternate which provider goes first each retry so a bad yt-dlp URL isn't re-picked
        // attempt after attempt.
        var providers = attempt % 2 == 0 ? _providerStrategy : Enumerable.Reverse(_providerStrategy);
        foreach (var strategy in providers)
        {
            var result = await ExecuteWithTimeout(strategy, url, TimeSpan.FromSeconds(15), cancellationToken);
            if (result.Success)
            {
                _logger.LogInformation("Successfully obtained stream URL using {Provider}",
                    strategy.Method.Name.Replace("TryGetWith", "").Replace("Async", ""));
                return result.Url!;
            }
        }

        _logger.LogError("All audio stream providers failed for: {Url}", url);
        throw new InvalidOperationException("No suitable audio format found from any provider.");
    }

    private async Task<(bool Success, string? Url)> ExecuteWithTimeout(
        Func<string, CancellationToken, Task<(bool, string?)>> func,
        string url,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            return await func(url, cancellationToken).WaitAsync(timeout, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException)
        {
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

    private async Task<(bool Success, string? Url)> TryGetWithYtDlpAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var ytdl = new YoutubeDL { YoutubeDLPath = "yt-dlp" };
            var overrideOptions = new OptionSet
            {
                Format = "bestaudio/best",
                ExtractorArgs = new MultiValue<string>(_potProviderExtractorArg, PlayerClientExtractorArg)
            };
            var result = await ytdl.RunVideoDataFetch(url, ct: cancellationToken, overrideOptions: overrideOptions);

            if (!result.Success || result.Data?.Formats == null)
            {
                _logger.LogWarning("YT-DLP fetch failed for: {Url}. Errors: {Errors}", url,
                    string.Join("; ", result.ErrorOutput ?? []));
                return (false, null);
            }

            var httpsFormats = result.Data.Formats
                .Where(f => f.Protocol != "mhtml")
                .AsQueryable();

            var bestAudio = httpsFormats
                                .MaxBy(f => f.AudioBitrate ?? 0)
                            ?? httpsFormats
                                .MaxBy(f => f.Bitrate ?? 0);

            if (bestAudio?.Url == null)
            {
                _logger.LogWarning("YT-DLP found no suitable audio format for: {Url}", url);
                return (false, null);
            }

            return (true, bestAudio.Url);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YT-DLP failed for: {Url}", url);
            return (false, null);
        }
    }

    private async Task<(bool Success, string? Url)> TryGetWithYoutubeExplodeAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
            var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(url, cancellationToken);
            var audioStream = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            return (true, audioStream.Url);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YoutubeExplode failed for: {Url}", url);
            return (false, null);
        }
    }

    public async Task<string> GetVideoTitleAsync(string url, CancellationToken cancellationToken)
    {
        var (success, title) = await ExecuteWithTimeout(TryGetTitleWithYtDlpAsync, url, TimeSpan.FromSeconds(15), cancellationToken);
        if (success)
        {
            return title!;
        }

        return (await _youtubeClient.Videos.GetAsync(url, cancellationToken)).Title;
    }

    private async Task<(bool Success, string? Url)> TryGetTitleWithYtDlpAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var ytdl = new YoutubeDL { YoutubeDLPath = "yt-dlp" };
            var overrideOptions = new OptionSet
            {
                ExtractorArgs = new MultiValue<string>(_potProviderExtractorArg, PlayerClientExtractorArg)
            };
            var result = await ytdl.RunVideoDataFetch(url, ct: cancellationToken, overrideOptions: overrideOptions);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Data?.Title))
            {
                _logger.LogWarning("YT-DLP title fetch failed for: {Url}. Errors: {Errors}", url,
                    string.Join("; ", result.ErrorOutput ?? []));
                return (false, null);
            }

            return (true, result.Data.Title);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YT-DLP title fetch failed for: {Url}", url);
            return (false, null);
        }
    }
}
