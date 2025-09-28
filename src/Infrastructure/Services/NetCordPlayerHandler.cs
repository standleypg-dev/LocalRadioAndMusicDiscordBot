using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Microsoft.Extensions.Logging;
using NetCord.Gateway.Voice;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

public class NetCordPlayerHandler(
    IMusicQueueService queue,
    ILogger<NetCordPlayerHandler> logger,
    INetCordAudioPlayerService playerService,
    PlayerState<VoiceClient> playerState)
    : IEventHandler<EventType.Play>, IEventHandler<EventType.Stop>, IEventHandler<EventType.Skip>
{
    public async void Handle(EventType.Play @event)
    {
        if(playerState.CurrentAction != PlayerAction.Stop)
        {
            logger.LogInformation("Play event received - but already playing");
            return;
        }
        await HandlePlayNextAsync();
    }
    
    public async void Handle(EventType.Skip @event)
    {
        logger.LogInformation("Skip event received - cancelling current play");
        
        // 1 indicates only the currently playing item
        // skip action should be ignored
        if(queue.Count <= 1)
        {
            logger.LogInformation("Skip event received - but queue is empty, ignored");
            return;
        }
        
        await playerState.SkipCts?.CancelAsync()!;
        playerState.SkipCts?.Dispose();
        playerState.SkipCts = null;
        
        queue.DequeueAsync(CancellationToken.None);
        
        playerState.CurrentAction = PlayerAction.Skip;
        await HandlePlayNextAsync();
    }
    
    public async void Handle(EventType.Stop @event)
    {
        await DisconnectVoiceClient();
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

                playerService.NotInVoiceChannelCallback += async () =>
                {
                    await callbacks.Invoke();
                };
                
                playerService.DisconnectedVoiceClientEvent += async () =>
                {
                    logger.LogInformation("Voice client disconnected - stopping playback");
                    await DisconnectVoiceClient();
                };
                
                await playerService.Play(context, SetDisconnectCallback);
                
                queue.DequeueAsync(CancellationToken.None);
            } while (!playerState.StopCts.Token.IsCancellationRequested && queue.Count > 0);
            
            if (queue.Count == 0)
            {
                logger.LogInformation("Queue is empty - stopping playback");
                await DisconnectVoiceClient();
            }
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
    
    private async Task DisconnectVoiceClient()
    {
        logger.LogInformation("Stop event received - cancelling loop");
        if (playerState.DisconnectAsyncCallback is not null)
        {
            await playerState.DisconnectAsyncCallback();
        }

        await playerState.StopCts?.CancelAsync()!;
        playerState.StopCts?.Dispose();
        playerState.StopCts = null;
        playerState.CurrentAction = PlayerAction.Stop;
        playerState.DisconnectAsyncCallback = null;
        playerState.CurrentVoiceClient = null;
    }
}