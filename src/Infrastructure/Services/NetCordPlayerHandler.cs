using System.Threading.Channels;
using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Microsoft.Extensions.Logging;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

public class NetCordPlayerHandler(
    IMusicQueueService queue,
    ILogger<NetCordPlayerHandler> logger,
    INetCordAudioPlayerService playerService,
    PlayerState playerState)
    : IEventHandler<EventType.Play>, IEventHandler<EventType.Stop>, IEventHandler<EventType.Skip>
{
    public async void Handle(EventType.Play @event)
    {
        if(playerState.IsPlaying)
        {
            logger.LogInformation("Play event received - but already playing");
            return;
        }
        await HandlePlayNextAsync();
    }

    public async void Handle(EventType.Stop @event)
    {
        logger.LogInformation("Stop event received - cancelling loop");
        if (playerState.DisconnectAsyncCallback is not null)
        {
            await playerState.DisconnectAsyncCallback();
        }

        await playerState.StopCts?.CancelAsync()!;
        playerState.StopCts?.Dispose();
        playerState.StopCts = null;
        playerState.IsPlaying = false;
        playerState.DisconnectAsyncCallback = null;
    }

    public async void Handle(EventType.Skip @event)
    {
        logger.LogInformation("Skip event received - cancelling current play");
        await playerState.SkipCts?.CancelAsync()!;
        playerState.SkipCts?.Dispose();
        playerState.SkipCts = null;
        
        queue.DequeueAsync(CancellationToken.None);
        
        await HandlePlayNextAsync();
    }

    private async Task HandlePlayNextAsync()
    {
        try
        {
            playerState.StopCts ??= new CancellationTokenSource();

            var request = queue.Peek<StringMenuInteractionContext>();
            if (request is null)
            {
                logger.LogInformation("No next item in queue");
                return;
            }

            var (context, callbacks) = request;

            playerState.IsPlaying = true;

            do
            {
                playerState.SkipCts?.Dispose();
                playerState.SkipCts = CancellationTokenSource.CreateLinkedTokenSource(playerState.StopCts.Token);

                playerState.StopCts.Token.Register(async void () =>
                {
                    try
                    {
                        context.Client.Disconnect += _ =>
                        {
                            logger.LogInformation("Client disconnected");
                            return default;
                        };
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error during disconnect");
                    }
                });

                var tokens = new TokenContainer
                {
                    SkipToken = playerState.SkipCts.Token,
                    StopToken = playerState.StopCts.Token
                };

                await playerService.Play(context, callbacks, SetDisconnectCallback, tokens);
            } while (!playerState.StopCts.Token.IsCancellationRequested && queue.Count > 0);
        }
        catch (OperationCanceledException) when (playerState.StopCts?.Token.IsCancellationRequested == true)
        {
            logger.LogInformation("Player loop was cancelled (Stop event)");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in NetCordPlayerHandler");
        }
    }

    private void SetDisconnectCallback(Func<Task> onDisconnectAsync)
    {
        playerState.DisconnectAsyncCallback = onDisconnectAsync;
    }
}