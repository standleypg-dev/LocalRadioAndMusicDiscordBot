using Application.DTOs;
using Application.Interfaces.Services;
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
    ILogger<AudioPlayerService> logger) : INetCordAudioPlayerService
{
    private VoiceClient? _voiceClient;
    private GatewayClient? _gatewayClient;
    private ulong? _guildId;

    public async Task<TrackPlayResult> PlayTrackAsync(PlayRequest request, int attempt, CancellationToken cancellationToken)
    {
        if (request is not PlayRequest<StringMenuInteractionContext> track)
        {
            throw new ArgumentException(
                $"Invalid request type. Expected {typeof(PlayRequest<StringMenuInteractionContext>)}",
                nameof(request));
        }

        var context = track.Context;
        var guild = context.Guild;
        if (guild is null || !guild.VoiceStates.TryGetValue(context.User.Id, out var voiceState))
        {
            return TrackPlayResult.NotInVoiceChannel;
        }

        try
        {
            if (_voiceClient is null)
            {
                await ConnectAsync(context.Client, guild.Id, voiceState.ChannelId.GetValueOrDefault(),
                    cancellationToken);
            }

            using var scope = serviceProvider.CreateScope();
            var streamService = scope.ServiceProvider.GetRequiredKeyedService<IStreamService>(nameof(YoutubeService));
            var radioSourceService = scope.ServiceProvider.GetRequiredService<IRadioSourceService>();
            var statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();

            var selectedValue = track.VideoUrl ?? context.SelectedValues[0];
            var (sourceUrl, song) = await ResolveSourceAsync(selectedValue, track, context.User.Id, attempt,
                streamService, radioSourceService, logger, cancellationToken);

            var process = await ffmpegProcessService.CreateStreamAsync(sourceUrl, cancellationToken);
            try
            {
                await statisticsService.LogSongPlayAsync(context.User.Id, context.User.Username,
                    context.User.GlobalName ?? string.Empty, song);

                var voiceClient = _voiceClient;
                if (voiceClient is null)
                {
                    logger.LogError("Voice client is no longer connected");
                    return TrackPlayResult.Failed;
                }

                var outStream = voiceClient.CreateVoiceStream();
                var opusStream = new OpusEncodeStream(outStream, PcmFormat.Short, VoiceChannels.Stereo,
                    OpusApplication.Audio);

                await process.StandardOutput.BaseStream.CopyToAsync(opusStream, cancellationToken);
                // Flush to make sure all the data has been sent and to indicate to Discord that we have finished sending
                await opusStream.FlushAsync(cancellationToken);

                await process.WaitForExitAsync(cancellationToken);
                return process.ExitCode == 0 ? TrackPlayResult.Completed : TrackPlayResult.Failed;
            }
            finally
            {
                await ffmpegProcessService.StopCurrentProcessAsync();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return TrackPlayResult.Skipped;
        }
    }

    public async Task DisconnectAsync()
    {
        var voiceClient = _voiceClient;
        var gatewayClient = _gatewayClient;
        var guildId = _guildId;
        _voiceClient = null;
        _gatewayClient = null;
        _guildId = null;

        try
        {
            if (gatewayClient is not null && guildId is not null)
            {
                await gatewayClient.UpdateVoiceStateAsync(new VoiceStateProperties(guildId.Value, null));
            }

            if (voiceClient is not null)
            {
                await voiceClient.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while disconnecting voice client");
        }
    }

    private async Task ConnectAsync(GatewayClient client, ulong guildId, ulong channelId,
        CancellationToken cancellationToken)
    {
        var voiceClient = await client.JoinVoiceChannelAsync(
            guildId,
            channelId,
            new VoiceClientConfiguration
            {
                Logger = new ConsoleLogger(),
            }, cancellationToken: cancellationToken);

        voiceClient.Disconnect += _ =>
        {
            logger.LogInformation("Voice client disconnected");
            // Drop the cached client so the next track triggers a fresh join.
            _voiceClient = null;
            return default;
        };

        await voiceClient.StartAsync(cancellationToken);
        await voiceClient.EnterSpeakingStateAsync(
            new SpeakingProperties(SpeakingFlags.Microphone),
            cancellationToken: cancellationToken);

        _voiceClient = voiceClient;
        _gatewayClient = client;
        _guildId = guildId;
    }

    private static async Task<(string SourceUrl, SongDtoBase Song)> ResolveSourceAsync(
        string selectedValue,
        PlayRequest track,
        ulong userId,
        int attempt,
        IStreamService streamService,
        IRadioSourceService radioSourceService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(selectedValue, out var radioId))
        {
            var radio = await radioSourceService.GetRadioSourceByIdAsync(radioId, cancellationToken);
            return (radio.SourceUrl, new SongDtoBase
            {
                Url = radio.SourceUrl,
                Title = radio.Name,
                UserId = userId
            });
        }

        var sourceUrl = await streamService.GetAudioStreamUrlAsync(selectedValue, attempt, cancellationToken);

        string title;
        if (track.VideoTitle != null)
        {
            title = track.VideoTitle;
        }
        else
        {
            try
            {
                title = await streamService.GetVideoTitleAsync(selectedValue, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch title for {Url}, using fallback title", selectedValue);
                title = "Unknown Title";
            }
        }

        return (sourceUrl, new SongDtoBase
        {
            Url = selectedValue,
            Title = title,
            UserId = userId
        });
    }
}
