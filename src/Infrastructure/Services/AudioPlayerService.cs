using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

public class AudioPlayerService(
    INativePlaceMusicProcessorService ffmpegProcessService,
    IServiceProvider serviceProvider,
    ILogger<AudioPlayerService> logger,
    PlayerState<VoiceClient> playerState,
    IMusicQueueService queue) : INetCordAudioPlayerService
{
    public event Func<Task>? DisconnectedVoiceClientEvent;
    public event Func<Task>? NotInVoiceChannelCallback;
    private Action<Func<Task>> OnDisconnectAsync { get; set; } = _ => { };

    private readonly int _maxRetryCount = 3;
    private PlayRequest<StringMenuInteractionContext>? _currentTrack;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Play(Action<Func<Task>> onDisconnectAsync)
    {
        OnDisconnectAsync = onDisconnectAsync;

        await HandleMusicPlayingAsync();
    }

    private async Task HandleMusicPlayingAsync()
    {
        logger.LogInformation("Entering semaphore to handle music playing");
        await _semaphore.WaitAsync();
        try
        {
            var ct = queue.Peek<StringMenuInteractionContext>();

            if (ct is null)
            {
                logger.LogError("Current track is null");
                return;
            }

            _currentTrack = ct;

            var guild = _currentTrack.Context.Guild!;
            // Get the user voice state
            if (!guild.VoiceStates.TryGetValue(_currentTrack.Context.User.Id, out var voiceState))
            {
                await (NotInVoiceChannelCallback?.Invoke() ?? Task.CompletedTask);
                return;
            }

            var client = _currentTrack.Context.Client;

            if (playerState.CurrentAction == PlayerAction.Stop)
            {
                await StartVoiceClientAsync();
            }

            if (playerState.CurrentVoiceClient is null)
            {
                logger.LogError("Voice client is null");
                return;
            }

            await HandleVoiceStream();

            async Task HandleVoiceStream()
            {
                var outStream = playerState.CurrentVoiceClient.CreateOutputStream();

                OpusEncodeStream stream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);

                using var scope = serviceProvider.CreateScope();
                var youTubeService =
                    scope.ServiceProvider.GetRequiredKeyedService<IStreamService>(nameof(YoutubeService));
                var radioSourceService = scope.ServiceProvider
                    .GetRequiredService<IRadioSourceService>();

                var selectedValue = _currentTrack.VideoUrl ?? _currentTrack.Context.SelectedValues[0];
                var sourceUrl = await GetSourceUrl(selectedValue, radioSourceService, youTubeService, _currentTrack.Context.User.Id, _currentTrack.Context.User.Username, _currentTrack.Context.User.GlobalName);

                await StartFfmpegStream(sourceUrl, stream);
            }

            async Task StartVoiceClientAsync()
            {
                playerState.CurrentVoiceClient = await client.JoinVoiceChannelAsync(
                    guild.Id,
                    voiceState.ChannelId.GetValueOrDefault(),
                    new VoiceClientConfiguration
                    {
                        Logger = new ConsoleLogger(),
                    }, cancellationToken: playerState.StopCts.Token);

                playerState.CurrentVoiceClient.Disconnect += HandleOnVoiceClientDisconnectedAsync;

                await playerState.CurrentVoiceClient.StartAsync(playerState.StopCts.Token);

                // Register the disconnect callback
                // This will be called when the cancellation token is triggered or when the voice client is closed
                OnDisconnectAsync(DisconnectVoiceClientAsync);

                await playerState.CurrentVoiceClient.EnterSpeakingStateAsync(
                    new SpeakingProperties(SpeakingFlags.Microphone),
                    cancellationToken: playerState.StopCts.Token);
            }

            async Task DisconnectVoiceClientAsync()
            {
                try
                {
                    await client.UpdateVoiceStateAsync(
                        new VoiceStateProperties(_currentTrack.Context.Guild!.Id, null),
                        null,
                        playerState.StopCts.Token);
                    await playerState.CurrentVoiceClient.CloseAsync(
                        cancellationToken: playerState.StopCts.Token);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                finally
                {
                    _currentTrack.RetryCount = 0;
                }
            }
        }
        finally
        {
            _semaphore.Release();
            logger.LogInformation("Released semaphore after handling music playing");
        }
    }

    private async Task StartFfmpegStream(string sourceUrl, OpusEncodeStream stream)
    {
        var ffmpeg =
            await ffmpegProcessService.CreateStreamAsync(sourceUrl,
                playerState.SkipCts.Token);

        ffmpegProcessService.OnForbiddenUrlRequest += OnForbiddenUrlRequest;
        ffmpegProcessService.OnPlaySongCompleted += OnPlaySongCompleted;

        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream,
            playerState.SkipCts.Token);

        // Flush 'stream' to make sure all the data has been sent and to indicate to Discord that we have finished sending
        await stream.FlushAsync(playerState.SkipCts.Token);
    }

    private async Task<string> GetSourceUrl(string selectedValue, IRadioSourceService radioSourceService,
        IStreamService youTubeService, ulong userId, string userName, string? globalName)
    {
        string sourceUrl;

        if (Guid.TryParse(selectedValue, out var radioId))
        {
            sourceUrl = (await radioSourceService.GetRadioSourceByIdAsync(radioId)).SourceUrl;
        }
        else
        {
            try
            {
                sourceUrl = await youTubeService.GetAudioStreamUrlAsync(selectedValue,
                    playerState.SkipCts.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting audio stream URL from YouTubeService for URL: {Url}", selectedValue);
                throw;
            }

            var song = new SongDtoBase
            {
                Url = selectedValue,
                Title = await youTubeService.GetVideoTitleAsync(selectedValue,
                    playerState.SkipCts.Token),
                UserId = userId
            };
            ffmpegProcessService.OnProcessStart += () => HandleOnProcessStartAsync(song, userId, userName, globalName);
        }

        return sourceUrl;
    }

    private async Task HandleOnProcessStartAsync(SongDtoBase song, ulong userId, string userName,string? globalName)
    {
        using var scope = serviceProvider.CreateScope();
        var statisticsService = scope.ServiceProvider
            .GetRequiredService<IStatisticsService>();
        await statisticsService
            .LogSongPlayAsync(userId, userName, globalName ?? string.Empty, song)
            .ConfigureAwait(false);

        playerState.CurrentAction = PlayerAction.Play;
    }

    private async Task OnPlaySongCompleted()
    {
        var queuePeek = queue.Peek<StringMenuInteractionContext>();
        if (queuePeek?.Id == _currentTrack?.Id)
        {
            logger.LogInformation($"Dequeuing track after completion: {_currentTrack?.Id}");
            queue.DequeueAsync(CancellationToken.None);
        }

        if (queue.Count == 0)
        {
            await (DisconnectedVoiceClientEvent?.Invoke() ?? Task.CompletedTask);
        }
    }

    private async ValueTask HandleOnVoiceClientDisconnectedAsync(DisconnectEventArgs args)
    {
        ffmpegProcessService.OnForbiddenUrlRequest -= OnForbiddenUrlRequest;
        ffmpegProcessService.OnPlaySongCompleted -= OnPlaySongCompleted;
        await (DisconnectedVoiceClientEvent?.Invoke() ?? Task.CompletedTask);
    }

    private async Task OnForbiddenUrlRequest()
    {
        await _semaphore.WaitAsync();
        try
        {
            // Retry to get new stream URL and play again
            if (_currentTrack is null || playerState.CurrentAction == PlayerAction.Stop)
            {
                logger.LogError("Current track is null or player is stopping, cannot retry");
                return;
            }

            _currentTrack.RetryCount++;
            if (_currentTrack.RetryCount > _maxRetryCount)
            {
                logger.LogError("Ffmpeg error received, maximum retry count reached, stopping playback");
                _currentTrack.RetryCount = 0;
                logger.LogInformation("Dequeuing track after max retries reached");
                queue.DequeueAsync(CancellationToken.None);
                if (queue.Count == 0)
                {
                    try
                    {
                        await (DisconnectedVoiceClientEvent?.Invoke() ?? Task.CompletedTask);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during disconnect event handling");
                        playerState.CurrentAction = PlayerAction.Stop;
                    }
                }

                return;
            }

            logger.LogWarning(
                $"Ffmpeg error received, retrying to play the stream: attempt {_currentTrack.RetryCount}/{_maxRetryCount}");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}