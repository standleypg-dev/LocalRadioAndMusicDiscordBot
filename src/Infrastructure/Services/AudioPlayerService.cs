using Application.DTOs;
using Application.Interfaces.Services;
using Application.Store;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class AudioPlayerService(
    GlobalStore globalStore,
    ILogger<AudioPlayerService> logger,
    INativePlaceMusicProcessorService ffmpegProcessService,
    IQueueService<SongDto<SocketVoiceChannel>> queueService,
    IServiceProvider serviceProvider) : IAudioPlayerService<SongDto<SocketVoiceChannel>, SocketVoiceChannel>
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));
    private readonly ILogger<AudioPlayerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Thread-safe cancellation management
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Lock _cancellationLock = new();

    // Flag to prevent recursive calls
    private bool _isProcessingNextSong;
    private readonly Lock _nextSongLock = new();

    public async Task InitiateVoiceChannelAsync(SongDto<SocketVoiceChannel> song, bool isYt = false)
    {
        var voiceChannel = song.VoiceChannel;
        var audioUrl = song.Url;
        var userId = song.UserId;
        try
        {
            using var scope = serviceProvider.CreateScope();
            var youtubeService = scope.ServiceProvider.GetRequiredService<IYoutubeService>();
            var statisticsService = scope.ServiceProvider
                .GetRequiredService<IStatisticsService<SocketUser, SongDto<SocketVoiceChannel>>>();

            var user = voiceChannel?.GetUser(userId);
            if (user is not null && isYt)
            {
                await statisticsService.LogSongPlayAsync(user.Id, user.Username, user.GlobalName, song);
            }

            // Update play state before starting
            _globalStore.Set(new PlayStateDto
            {
                IsPlaying = true,
                IsRadioPlaying = !isYt
            });

            string outputUrl;
            if (isYt)
            {
                outputUrl = await youtubeService.GetAudioStreamUrlAsync(audioUrl);
            }
            else
            {
                outputUrl = audioUrl;
            }

            await ConnectToVoiceChannelAsync(voiceChannel, outputUrl);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("Audio operation was cancelled: {Message}", ex.Message);
            await SetStoppedStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating voice channel connection");
            await SetStoppedStateAsync();
            throw; // Re-throw to allow caller to handle
        }
    }

    public async Task NextSongAsync()
    {
        // Prevent recursive calls and multiple simultaneous next song operations
        lock (_nextSongLock)
        {
            if (_isProcessingNextSong)
            {
                _logger.LogInformation("NextSongAsync already in progress, skipping");
                return;
            }

            _isProcessingNextSong = true;
        }

        try
        {
            _logger.LogInformation("Processing next song");

            // Skip current song from queue (this removes the currently playing song)
            await queueService.SkipSongAsync();

            // Cancel current audio stream safely
            await CancelCurrentAudioAsync();

            // Check if there are more songs in queue AFTER skipping
            var songQueue = _globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>();
            if (songQueue?.Count > 0)
            {
                // Get the next song (don't dequeue here - let the completion logic handle it)
                var nextSong = songQueue.Peek();
                _logger.LogInformation("Playing next song: {SongUrl}", nextSong.Url);

                // Start playing next song - use Task.Run to prevent stack overflow
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await InitiateVoiceChannelAsync(nextSong, isYt: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error starting next song");
                        await DestroyVoiceChannelAsync();
                    }
                }, CancellationToken.None);
            }
            else
            {
                _logger.LogInformation("No more songs in queue, destroying voice channel");
                await DestroyVoiceChannelAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing next song");
            await DestroyVoiceChannelAsync(); // Cleanup on error
        }
        finally
        {
            lock (_nextSongLock)
            {
                _isProcessingNextSong = false;
            }
        }
    }

    public async Task DestroyVoiceChannelAsync()
    {
        try
        {
            _logger.LogInformation("Destroying voice channel connection");

            // Cancel current audio operations (this signals Discord streams to clean up)
            await CancelCurrentAudioAsync();

            // Additional wait to ensure Discord streams have finished their natural disposal
            await Task.Delay(100);

            // Clean up audio client
            if (_globalStore.TryGet<IAudioClient>(out var audioClient))
            {
                try
                {
                    await audioClient.StopAsync();
                    audioClient.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning up audio client");
                }
                finally
                {
                    _globalStore.Clear<IAudioClient>();
                }
            }

            // Disconnect from voice channel
            if (_globalStore.TryGet<SocketVoiceChannel>(out var currentVoiceChannel))
            {
                try
                {
                    await currentVoiceChannel.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disconnecting from voice channel");
                }
                finally
                {
                    _globalStore.Clear<SocketVoiceChannel>();
                }
            }

            // Now safe to dispose cancellation token source
            lock (_cancellationLock)
            {
                if (_cancellationTokenSource != null)
                {
                    try
                    {
                        // By now, Discord streams should have disposed naturally
                        _cancellationTokenSource.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing cancellation token source");
                    }
                    finally
                    {
                        _cancellationTokenSource = null;
                    }
                }
            }

            await SetStoppedStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error destroying voice channel");

            // Try to notify user about the error
            try
            {
                var messageComponent = _globalStore.Get<SocketMessageComponent>();
                if (messageComponent != null)
                {
                    await messageComponent.FollowupAsync($"Error stopping audio: {ex.Message}");
                }
            }
            catch (Exception notifyEx)
            {
                _logger.LogWarning(notifyEx, "Could not notify user about destruction error");
            }
        }
    }

    public async Task OnPlaylistChanged()
    {
        try
        {
            var songs = _globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>();
            if (songs?.Count > 0)
            {
                var song = songs.Peek(); // Just peek, don't dequeue yet
                var playState = _globalStore.Get<PlayStateDto>() ??
                                new PlayStateDto { IsPlaying = false, IsRadioPlaying = false };

                // Only start playing if not already playing or if currently playing radio
                if (!playState.IsPlaying || playState.IsRadioPlaying)
                {
                    _logger.LogInformation("Playlist changed, starting new song");
                    await InitiateVoiceChannelAsync(song, isYt: true);
                }
            }
            else
            {
                _logger.LogInformation("Playlist is empty, destroying voice channel");
                await DestroyVoiceChannelAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playlist change");
        }
    }

    public SocketVoiceChannel? GetBotCurrentVoiceChannel()
    {
        return _globalStore.Get<SocketVoiceChannel>();
    }

    #region Private Methods

    private async Task ConnectToVoiceChannelAsync(SocketVoiceChannel? voiceChannel, string audioUrl)
    {
        IAudioClient? audioClient;
        Stream? audioOutStream = null;
        AudioOutStream? discord = null;
        BufferedStream? bufferedStream = null;

        try
        {
            // Cancel any existing audio operations
            await CancelCurrentAudioAsync();

            // Create new cancellation token (dispose old one safely first)
            lock (_cancellationLock)
            {
                // Dispose the old token source if it exists and is not being used by Discord streams
                if (_cancellationTokenSource != null)
                {
                    try
                    {
                        // Only dispose if it's already cancelled (meaning streams should be done with it)
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource.Dispose();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
                }

                _cancellationTokenSource = new CancellationTokenSource();
            }

            var cancellationToken = _cancellationTokenSource.Token;

            // Connect to voice channel if needed
            if (voiceChannel is not null)
            {
                // Ensure voice channel have at least one user
                if (voiceChannel.ConnectedUsers.Count(u => !u.IsBot) == 0)
                {
                    _logger.LogWarning("Voice channel {ChannelId} has no users, cannot connect", voiceChannel.Id);
                    var messageComponent = _globalStore.Get<SocketMessageComponent>();
                    if (messageComponent != null)
                    {
                        await messageComponent.FollowupAsync(
                            $"Can't connect to '{voiceChannel.Name}' — no users are in the channel. It looks like this command was used in a different voice channel earlier. Please use the command again in the correct channel.");
                    }

                    await SetStoppedStateAsync();
                    await queueService.ClearQueueAsync();
                    return;
                }

                // Disconnect from current channel first
                if (_globalStore.TryGet<SocketVoiceChannel>(out var currentVoiceChannel) &&
                    currentVoiceChannel.Id != voiceChannel.Id)
                {
                    await currentVoiceChannel.DisconnectAsync();
                }

                // Check if we already have an connected voice channel, then reuse it
                if (_globalStore.TryGet(out audioClient) &&
                    audioClient.ConnectionState == ConnectionState.Connected)
                {
                    _logger.LogInformation("Reusing existing audio client for channel: {ChannelId}", voiceChannel.Id);
                }
                else
                {
                    // Connect to new channel
                    _logger.LogInformation("Connecting to new voice channel: {ChannelId}", voiceChannel.Id);
                    audioClient = await voiceChannel.ConnectAsync();
                    _globalStore.Set<IAudioClient>(audioClient);
                    _globalStore.Set(voiceChannel);
                }
            }
            else if (_globalStore.TryGet(out audioClient))
            {
                // Use existing audio client
            }
            else
            {
                throw new InvalidOperationException("No voice channel provided and no existing connection");
            }

            // Create FFmpeg process and streams
            var process = await ffmpegProcessService.CreateStream(audioUrl, cancellationToken);
            audioOutStream = process.StandardOutput.BaseStream;
            discord = audioClient.CreatePCMStream(AudioApplication.Music);
            bufferedStream = new BufferedStream(discord, 4096); // Increased buffer size

            _logger.LogInformation("Starting audio stream from: {AudioUrl}", audioUrl);

            // Copy audio data to Discord
            await audioOutStream.CopyToAsync(bufferedStream, cancellationToken);
            await bufferedStream.FlushAsync(cancellationToken);

            _logger.LogInformation("Audio stream completed");

            // Check for next song only if not cancelled
            if (!cancellationToken.IsCancellationRequested)
            {
                var songQueue = _globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>();
                if (songQueue?.Count > 0)
                {
                    var completedSong = songQueue.Peek();
                    _logger.LogInformation("Completed song: {SongUrl}", completedSong.Url);

                    // Check if there are more songs after dequeuing
                    if (songQueue.Count > 0)
                    {
                        // Use Task.Run to avoid deep recursion
                        _ = Task.Run(async () => await NextSongAsync(), CancellationToken.None);
                    }
                    else
                    {
                        _logger.LogInformation("Queue is now empty after completing song");
                        await DestroyVoiceChannelAsync();
                    }
                }
                else
                {
                    _logger.LogInformation("No songs in queue");
                    await DestroyVoiceChannelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audio stream was cancelled");
            // Don't treat cancellation as an error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audio connection");
            await DestroyVoiceChannelAsync();
            throw;
        }
        finally
        {
            // Proper resource cleanup order - DO NOT dispose cancellation token here
            // Flush streams before any disposal
            try
            {
                if (bufferedStream != null)
                {
                    await bufferedStream.FlushAsync(CancellationToken.None);
                }

                if (discord != null)
                {
                    await discord.FlushAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error flushing streams during cleanup");
            }

            // Dispose non-Discord streams first
            try
            {
                bufferedStream?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing buffered stream");
            }

            try
            {
                audioOutStream?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing audio output stream");
            }

            // Let Discord stream dispose naturally via cancellation
            // Don't explicitly dispose - it will clean up when it detects cancellation
            if (discord != null)
            {
                // Optional: Set a flag or log that we're letting it dispose naturally
                _logger.LogDebug("Allowing Discord stream to dispose naturally via cancellation");
            }

            // Note: audioClient is managed by GlobalStore
            // Note: cancellationTokenSource will be disposed later in DestroyVoiceChannelAsync
        }
    }

    /// <summary>
    /// Safely cancels current audio operations without disposing the token source immediately
    /// </summary>
    private async Task CancelCurrentAudioAsync()
    {
        CancellationTokenSource? tokenSource = null;

        lock (_cancellationLock)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                tokenSource = _cancellationTokenSource;
            }
        }

        if (tokenSource != null)
        {
            try
            {
                // Cancel the token - Discord streams will detect this and clean up
                await tokenSource.CancelAsync();

                // Give Discord streams time to detect cancellation and dispose naturally
                await Task.Delay(200, CancellationToken.None);

                _logger.LogDebug("Cancellation signal sent, Discord streams disposing naturally");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during cancellation");
            }
        }
    }

    /// <summary>
    /// Sets the play state to stopped
    /// </summary>
    private async Task SetStoppedStateAsync()
    {
        _globalStore.Set(new PlayStateDto
        {
            IsPlaying = false,
            IsRadioPlaying = false
        });

        // Small delay to ensure state is persisted
        await Task.Delay(10);
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Cancel ongoing operations
            lock (_cancellationLock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }

    #endregion
}