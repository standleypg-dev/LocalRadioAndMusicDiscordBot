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
    ILogger<NetCordAudioPlayerService> logger) : INetCordAudioPlayerService
{
    public async Task Play<T>(T ctx, Func<Task> notInVoiceChannelCallback,
        Action<Func<Task>> onDisconnectAsync,
        TokenContainer tokens)
    {
        if (ctx is not StringMenuInteractionContext context)
        {
            throw new ArgumentException("Invalid context type. Expected StringMenuInteractionContext.", nameof(ctx));
        }

        await HandleMusicPlayingAsync(context, notInVoiceChannelCallback, onDisconnectAsync, tokens);
    }

    private async Task HandleMusicPlayingAsync(StringMenuInteractionContext context,
        Func<Task> notInVoiceChannelCallback,
        Action<Func<Task>> onDisconnectAsync,
        TokenContainer tokens)
    {
        var guild = context.Guild!;
        // Get the user voice state
        if (!guild.VoiceStates.TryGetValue(context.User.Id, out var voiceState))
        {
            await notInVoiceChannelCallback.Invoke();
            return;
        }

        var client = context.Client;

        var voiceClient = await client.JoinVoiceChannelAsync(
            guild.Id,
            voiceState.ChannelId.GetValueOrDefault(),
            new VoiceClientConfiguration
            {
                Logger = new ConsoleLogger(),
            }, cancellationToken: tokens.StopToken);

        await voiceClient.StartAsync(tokens.StopToken);
        
        // Register the disconnect callback
        // This will be called when the cancellation token is triggered or when the voice client is closed
        onDisconnectAsync(DisconnectAsync);
        
        await voiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone),
            cancellationToken: tokens.StopToken);

        var outStream = voiceClient.CreateOutputStream();

        OpusEncodeStream stream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);

        // From KeyedService
        var scope = serviceProvider.CreateScope();
        var soundCloudService =
            scope.ServiceProvider.GetRequiredKeyedService<IStreamService>(nameof(SoundCloudService));
        var url = await soundCloudService.GetAudioStreamUrlAsync(context.SelectedValues[0], tokens.StopToken);

        var ffmpeg = await ffmpegProcessService.CreateStreamAsync(url, tokens.SkipToken);

        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream, tokens.StopToken);

        // Flush 'stream' to make sure all the data has been sent and to indicate to Discord that we have finished sending
        await stream.FlushAsync(tokens.StopToken);

        async Task DisconnectAsync()
        {
            await client.UpdateVoiceStateAsync(
                new VoiceStateProperties(context.Guild!.Id, null),
                null,
                tokens.StopToken);
            await voiceClient.CloseAsync(cancellationToken: tokens.StopToken);
        }
    }
}