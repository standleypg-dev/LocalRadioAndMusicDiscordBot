using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

public class NetCordAudioPlayerService(
    INativePlaceMusicProcessorService ffmpegProcessService,
    IServiceProvider serviceProvider,
    ILogger<NetCordAudioPlayerService> logger,
    PlayerState<VoiceClient> playerState,
    IMusicQueueService queue) : INetCordAudioPlayerService
{
    public event Func<Task>? DisconnectedVoiceClientEvent;
    public event Func<Task>? NotInVoiceChannelCallback;
    private Action<Func<Task>> OnDisconnectAsync { get; set; } = _ => { };
    private StringMenuInteractionContext Context { get; set; } = null!;

    public async Task Play<T>(T ctx, Action<Func<Task>> onDisconnectAsync)
    {
        if (ctx is not StringMenuInteractionContext context)
        {
            throw new ArgumentException("Invalid context type. Expected StringMenuInteractionContext.", nameof(ctx));
        }

        OnDisconnectAsync = onDisconnectAsync;
        Context = context;

        await HandleMusicPlayingAsync();
    }

    private async Task HandleMusicPlayingAsync()
    {
        var guild = Context.Guild!;
        // Get the user voice state
        if (!guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
        {
            await (NotInVoiceChannelCallback?.Invoke() ?? Task.CompletedTask);
            return;
        }

        var client = Context.Client;

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

            var currentTrack = queue.Peek<StringMenuInteractionContext>();
            if (currentTrack is null)
            {
                logger.LogError("Current track is null");
                return;
            }

            var selectedValue = currentTrack.Context.SelectedValues[0];
            string sourceUrl;

            if (Guid.TryParse(selectedValue, out var radioId))
            {
                sourceUrl = (await radioSourceService.GetRadioSourceByIdAsync(radioId)).SourceUrl;
            }
            else
            {
                sourceUrl = await youTubeService.GetAudioStreamUrlAsync(selectedValue,
                    playerState.SkipCts?.Token ?? CancellationToken.None);
                
                var song = new SongDtoBase
                {
                    Url = selectedValue,
                    Title = await youTubeService.GetVideoTitleAsync(selectedValue,
                        playerState.SkipCts?.Token ?? CancellationToken.None),
                    UserId = currentTrack.Context.User.Id
                };
                ffmpegProcessService.OnProcessStart += () => HandleOnProcessStartAsync(song);
            }

            var ffmpeg =
                await ffmpegProcessService.CreateStreamAsync(sourceUrl,
                    playerState.SkipCts?.Token ?? CancellationToken.None);
            

            ffmpegProcessService.OnExitProcess += HandleOnProcessExitAsync;

            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream,
                playerState.SkipCts?.Token ?? CancellationToken.None);

            // Flush 'stream' to make sure all the data has been sent and to indicate to Discord that we have finished sending
            await stream.FlushAsync(playerState.SkipCts?.Token ?? CancellationToken.None);
        }

        async Task StartVoiceClientAsync()
        {
            playerState.CurrentVoiceClient = await client.JoinVoiceChannelAsync(
                guild.Id,
                voiceState.ChannelId.GetValueOrDefault(),
                new VoiceClientConfiguration
                {
                    Logger = new ConsoleLogger(),
                }, cancellationToken: playerState.StopCts?.Token ?? CancellationToken.None);

            playerState.CurrentVoiceClient.Disconnect += HandleOnVoiceClientDisconnectedAsync;

            await playerState.CurrentVoiceClient.StartAsync(playerState.StopCts?.Token ?? CancellationToken.None);

            // Register the disconnect callback
            // This will be called when the cancellation token is triggered or when the voice client is closed
            OnDisconnectAsync(DisconnectVoiceClientAsync);

            await playerState.CurrentVoiceClient.EnterSpeakingStateAsync(
                new SpeakingProperties(SpeakingFlags.Microphone),
                cancellationToken: playerState.StopCts?.Token ?? CancellationToken.None);
        }

        async Task DisconnectVoiceClientAsync()
        {
            await client.UpdateVoiceStateAsync(
                new VoiceStateProperties(Context.Guild!.Id, null),
                null,
                playerState.StopCts?.Token ?? CancellationToken.None);
            await playerState.CurrentVoiceClient.CloseAsync(
                cancellationToken: playerState.StopCts?.Token ?? CancellationToken.None);
        }
    }

    private async Task HandleOnProcessStartAsync(SongDtoBase song)
    {
        using var scope = serviceProvider.CreateScope();
        var statisticsService = scope.ServiceProvider
            .GetRequiredService<IStatisticsService>();
        await statisticsService
            .LogSongPlayAsync(Context.User.Id, Context.User.Username, Context.User.GlobalName ?? string.Empty, song)
            .ConfigureAwait(false);

        playerState.CurrentAction = PlayerAction.Play;
        await Task.CompletedTask;
    }

    private async Task HandleOnProcessExitAsync()
    {
        if (queue.Count == 0)
        {
            await (DisconnectedVoiceClientEvent?.Invoke() ?? Task.CompletedTask);
        }
    }

    private async ValueTask HandleOnVoiceClientDisconnectedAsync(DisconnectEventArgs args)
    {
        await (DisconnectedVoiceClientEvent?.Invoke() ?? Task.CompletedTask);
    }
}