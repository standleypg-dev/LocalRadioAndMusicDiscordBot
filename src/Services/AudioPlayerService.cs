using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using radio_discord_bot.Utils;

namespace radio_discord_bot.Services;

public class AudioPlayerService(
    IYoutubeService youtubeService,
    GlobalStore globalStore,
    ILogger<AudioPlayerService> logger,
    IFfmpegProcessService ffmpegProcessService,
    IQueueService queueService) : IAudioPlayerService
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));
    private readonly ILogger<AudioPlayerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private CancellationTokenSource? _cancellationTokenSource;

    public async Task InitiateVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl, bool isYt = false)
    {
        try
        {
            _globalStore.Set(new PlayState
            {
                IsPlaying = true, IsRadioPlaying = !isYt
            });
            dynamic outputUrl = isYt
                ? await youtubeService.GetAudioStreamUrlAsync(audioUrl)
                : audioUrl;

            await ConnectToVoiceChannelAsync(voiceChannel, outputUrl);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Error on InitiateVoiceChannelAsync: {ex.Message}");
            _globalStore.Set(new PlayState
            {
                IsPlaying = false, IsRadioPlaying = false
            });
        }
    }

    public async Task NextSongAsync()
    {
        await queueService.SkipSongAsync();

        if (_cancellationTokenSource is not null && !_cancellationTokenSource.IsCancellationRequested)
        {
            await _cancellationTokenSource.CancelAsync();
        }

        if (_globalStore.Get<Queue<Song>>()?.Count > 0)
        {
            var song = _globalStore.Get<Queue<Song>>()?.Peek();
            if (song != null)
                await InitiateVoiceChannelAsync(song.VoiceChannel, song.Url, isYt: true);
        }
        else
        {
            await DestroyVoiceChannelAsync();
        }
    }

    public async Task DestroyVoiceChannelAsync()
    {
        try
        {
            if (_globalStore.TryGet<IVoiceChannel>(out var currentVoiceChannel))
            {
                await currentVoiceChannel.DisconnectAsync();
                _globalStore.Clear<IVoiceChannel>();
            }

            _globalStore.Set(new PlayState
            {
                IsPlaying = false, IsRadioPlaying = false
            });
            _globalStore.Clear<IAudioClient>();
        }
        catch (Exception ex)
        {
            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                $"Error on DestroyVoiceChannelAsync: {ex.Message}");
        }
    }

    public async Task OnPlaylistChanged()
    {
        var songs = _globalStore.Get<Queue<Song>>();
        if (songs is not null)
        {
            var song = songs.Peek();
            var playState = _globalStore.Get<PlayState>() ??
                            new PlayState { IsPlaying = false, IsRadioPlaying = false };
            if (!playState.IsPlaying || playState.IsRadioPlaying)
                await InitiateVoiceChannelAsync(song.VoiceChannel, song.Url, isYt: true);
        }
        else
        {
            await DestroyVoiceChannelAsync();
        }
    }

    public IVoiceChannel? GetBotCurrentVoiceChannel()
    {
        return _globalStore.Get<IVoiceChannel>();
    }

    #region Private Methods

    private async Task ConnectToVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl)
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _cancellationTokenSource.Token;

            List<Task> tasks = new();
            if (voiceChannel is not null)
            {
                if (_globalStore.TryGet<IVoiceChannel>(out var currentVoiceChannel))
                    await currentVoiceChannel.DisconnectAsync();

                _globalStore.Set<IAudioClient>(await voiceChannel.ConnectAsync(disconnect: false));
            }

            if (_globalStore.TryGet<IAudioClient>(out var audioClient))
            {
                var process = await ffmpegProcessService.CreateStream(audioUrl, cancellationToken);
                await using var audioOutStream = process.StandardOutput.BaseStream;
                await using var discord = audioClient.CreatePCMStream(AudioApplication.Music);
                await using var bufferedStream = new BufferedStream(discord, 2048);
                try
                {
                    // Store the current voice channel
                    _globalStore.Set(voiceChannel);

                    tasks.Add(audioOutStream.CopyToAsync(bufferedStream, cancellationToken));
                    tasks.Add(bufferedStream.FlushAsync(cancellationToken));

                    await Task.WhenAll(tasks);

                    if (_globalStore.Get<Queue<Song>>()?.Count > 0)
                        await NextSongAsync();
                    else
                        await DestroyVoiceChannelAsync();

                    audioClient.ClientDisconnected += async (e) =>
                    {
                        await DestroyVoiceChannelAsync();
                        logger.LogInformation("Disconnected from voice channel: {0}", e);
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on ConnectToVoiceChannelAsync");
                }
                finally
                {
                    await audioClient.StopAsync();
                    audioClient.Dispose();
                    await discord.FlushAsync(cancellationToken);
                    await bufferedStream.FlushAsync(cancellationToken);
                }
            }
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError($"Task Canceled Exception {exception.Message}.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion
}