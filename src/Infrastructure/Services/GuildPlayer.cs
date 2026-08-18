using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

/// <summary>
/// Owns playback for a single guild: the single consumer of that guild's music queue
/// (queue service pattern from
/// https://learn.microsoft.com/en-us/dotnet/core/extensions/queue-service).
/// Dequeues one request at a time, plays it to completion via
/// <see cref="INetCordAudioPlayerService"/>, retries failed tracks, and disconnects
/// from the guild's voice channel when the queue runs empty.
/// Created and driven by <see cref="GuildPlayerManager"/>.
/// </summary>
public sealed class GuildPlayer(
    IMusicQueueService queue,
    INetCordAudioPlayerService audioPlayer,
    ILogger logger)
{
    private const int MaxRetryCount = 6;

    private readonly Lock _ctsLock = new();
    private CancellationTokenSource? _trackCts;

    public IMusicQueueService Queue => queue;

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            PlayRequest<StringMenuInteractionContext> request;
            try
            {
                request = await queue.DequeueAsync<StringMenuInteractionContext>(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            queue.SetNowPlaying(request);
            CancellationToken trackToken;
            lock (_ctsLock)
            {
                _trackCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                trackToken = _trackCts.Token;
            }

            try
            {
                await PlayWithRetryAsync(request, trackToken);
            }
            finally
            {
                queue.SetNowPlaying(null);
                lock (_ctsLock)
                {
                    _trackCts.Dispose();
                    _trackCts = null;
                }
            }

            if (queue.Count == 0 && !stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Queue is empty - disconnecting from voice channel");
                try
                {
                    await audioPlayer.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error disconnecting voice client");
                }

                await SafeCallbackAsync(request,
                    "Disconnected from voice channel either due to inactivity or error encountered.");
            }
        }
    }

    private async Task PlayWithRetryAsync(PlayRequest request, CancellationToken trackToken)
    {
        for (var attempt = 1; attempt <= MaxRetryCount; attempt++)
        {
            TrackPlayResult result;
            try
            {
                result = await audioPlayer.PlayTrackAsync(request, attempt, trackToken);
            }
            catch (OperationCanceledException) when (trackToken.IsCancellationRequested)
            {
                logger.LogInformation("Track {TrackId} cancelled (skip/stop)", request.Id);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error playing track {TrackId} (attempt {Attempt}/{Max})",
                    request.Id, attempt, MaxRetryCount);
                result = TrackPlayResult.Failed;
            }

            switch (result)
            {
                case TrackPlayResult.Completed:
                case TrackPlayResult.Skipped:
                    return;

                case TrackPlayResult.NotInVoiceChannel:
                    await SafeCallbackAsync(request, "You are not connected to any voice channel!");
                    return;

                case TrackPlayResult.Failed when attempt < MaxRetryCount:
                    logger.LogWarning("Track {TrackId} failed, retrying: attempt {Attempt}/{Max}",
                        request.Id, attempt, MaxRetryCount);
                    continue;

                default:
                    logger.LogError("Track {TrackId} failed after {Max} attempts, dropping it",
                        request.Id, MaxRetryCount);
                    return;
            }
        }
    }

    public void Skip()
    {
        CancellationTokenSource? cts;
        lock (_ctsLock)
        {
            cts = _trackCts;
        }

        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The track finished between the read and the cancel; nothing to skip.
        }
    }

    public void Stop()
    {
        queue.Clear();
        Skip();
    }

    private async Task SafeCallbackAsync(PlayRequest request, string message)
    {
        try
        {
            await request.Callbacks.Invoke(message);
        }
        catch (Exception ex)
        {
            // An interaction can only be responded to once; a failed notification must not stop the player.
            logger.LogWarning(ex, "Failed to send message to Discord: {Message}", message);
        }
    }
}
