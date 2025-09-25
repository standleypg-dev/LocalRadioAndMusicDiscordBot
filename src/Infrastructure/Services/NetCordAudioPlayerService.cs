using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

public class NetCordAudioPlayerService(
    INativePlaceMusicProcessorService ffmpegProcessService,
    IServiceProvider serviceProvider) : INetCordAudioPlayerService
{
    public async Task Play<T>(T ctx, Func<Task> notInVoiceChannelCallback,
        CancellationToken cancellationToken)
    {
        if (ctx is not StringMenuInteractionContext context)
        {
            throw new ArgumentException("Invalid context type. Expected StringMenuInteractionContext.", nameof(ctx));
        }

        while (cancellationToken.IsCancellationRequested)
        {
            await HandleMusicPlayingAsync(context, notInVoiceChannelCallback, cancellationToken);
        }

    }

    private async Task HandleMusicPlayingAsync(StringMenuInteractionContext context,
        Func<Task> notInVoiceChannelCallback, CancellationToken cancellationToken)
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
            }, cancellationToken: cancellationToken);

        await voiceClient.StartAsync(cancellationToken);

        await voiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone),
            cancellationToken: cancellationToken);

        var outStream = voiceClient.CreateOutputStream();

        OpusEncodeStream stream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);

        // From KeyedService
        var scope = serviceProvider.CreateScope();
        var soundCloudService = scope.ServiceProvider.GetRequiredKeyedService<IStreamService>(nameof(SoundCloudService));
        var url = await soundCloudService.GetAudioStreamUrlAsync(context.SelectedValues[0]);

        var ffmpeg = await ffmpegProcessService.CreateStreamAsync(url, cancellationToken);

        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream, cancellationToken);

        // Flush 'stream' to make sure all the data has been sent and to indicate to Discord that we have finished sending
        await stream.FlushAsync(cancellationToken);
    }
}