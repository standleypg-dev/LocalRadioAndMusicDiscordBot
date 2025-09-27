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
    private Func<Task> DisconnectVoiceClient { get; set; } = () => Task.CompletedTask;
    private Action<Func<Task>> OnDisconnectAsync { get; set; } = _ => { };
    private Func<Task> NotInVoiceChannelCallback { get; set; } = () => Task.CompletedTask;
    private StringMenuInteractionContext CurrentContext { get; set; } = null!;
    public async Task Play<T>(T ctx, Func<Task> notInVoiceChannelCallback,
        Action<Func<Task>> onDisconnectAsync, Func<Task> disconnectVoiceClient)
    {
        if (ctx is not StringMenuInteractionContext context)
        {
            throw new ArgumentException("Invalid context type. Expected StringMenuInteractionContext.", nameof(ctx));
        }
        NotInVoiceChannelCallback = notInVoiceChannelCallback;
        OnDisconnectAsync = onDisconnectAsync;
        DisconnectVoiceClient = disconnectVoiceClient;
        CurrentContext = context;

        await HandleMusicPlayingAsync();
    }

    private async Task HandleMusicPlayingAsync()
    {
        var guild = CurrentContext.Guild!;
        // Get the user voice state
        if (!guild.VoiceStates.TryGetValue(CurrentContext.User.Id, out var voiceState))
        {
            await NotInVoiceChannelCallback.Invoke();
            return;
        }

        var client = CurrentContext.Client;

        if (playerState.CurrentAction == PlayerAction.Stop)
        {
            playerState.CurrentVoiceClient = await client.JoinVoiceChannelAsync(
                guild.Id,
                voiceState.ChannelId.GetValueOrDefault(),
                new VoiceClientConfiguration
                {
                    Logger = new ConsoleLogger(),
                }, cancellationToken: playerState.StopCts?.Token ?? CancellationToken.None);
            
            playerState.CurrentVoiceClient.Disconnect += async _ =>
            {
                await DisconnectVoiceClient();
            };
            
            await playerState.CurrentVoiceClient.StartAsync(playerState.StopCts?.Token ?? CancellationToken.None);
            
            // Register the disconnect callback
            // This will be called when the cancellation token is triggered or when the voice client is closed
            OnDisconnectAsync(DisconnectAsync);

            await playerState.CurrentVoiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone),
                cancellationToken: playerState.StopCts?.Token ?? CancellationToken.None);
        }
        
        if (playerState.CurrentVoiceClient is null)
        {
            logger.LogError("Voice client is null");
            return;
        }

        var outStream = playerState.CurrentVoiceClient.CreateOutputStream();

        OpusEncodeStream stream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);

        // From KeyedService
        using var scope = serviceProvider.CreateScope();
        var soundCloudService =
            scope.ServiceProvider.GetRequiredKeyedService<IStreamService>(nameof(SoundCloudService));
        var youTubeService =
            scope.ServiceProvider.GetRequiredKeyedService<IStreamService>(nameof(YoutubeService));
        
        var currentTrack =  queue.Peek<StringMenuInteractionContext>();
        if (currentTrack is null)
        {
            logger.LogError("Current track is null");
            return;
        }
        // var url = await soundCloudService.GetAudioStreamUrlAsync(currentTrack.Ctx.SelectedValues[0],
        //     playerState.SkipCts?.Token ?? CancellationToken.None);
        var url = await youTubeService.GetAudioStreamUrlAsync(currentTrack.Ctx.SelectedValues[0],
            playerState.SkipCts?.Token ?? CancellationToken.None);

        var ffmpeg =
            await ffmpegProcessService.CreateStreamAsync(url, DisconnectVoiceClient, playerState.SkipCts?.Token ?? CancellationToken.None);

        playerState.CurrentAction = PlayerAction.Play;

        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream,
            playerState.SkipCts?.Token ?? CancellationToken.None);

        // Flush 'stream' to make sure all the data has been sent and to indicate to Discord that we have finished sending
        await stream.FlushAsync(playerState.SkipCts?.Token ?? CancellationToken.None);

        async Task DisconnectAsync()
        {
            await client.UpdateVoiceStateAsync(
                new VoiceStateProperties(CurrentContext.Guild!.Id, null),
                null,
                playerState.StopCts?.Token ?? CancellationToken.None);
            await playerState.CurrentVoiceClient.CloseAsync(cancellationToken: playerState.StopCts?.Token ?? CancellationToken.None);
        }
    }
}