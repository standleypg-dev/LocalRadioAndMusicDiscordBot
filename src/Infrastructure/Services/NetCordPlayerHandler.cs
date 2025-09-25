using System.Threading.Channels;
using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Microsoft.Extensions.Logging;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Services;

public class NetCordPlayerHandler(
    Channel<PlayRequest<StringMenuInteractionContext>> channel,
    ILogger<NetCordPlayerHandler> logger,
    INetCordAudioPlayerService playerService)
    : IEventHandler<EventType.Play>, IEventHandler<EventType.Stop>, IEventHandler<EventType.Skip>
{
    // Cancels the whole consumer loop (Stop)
    private CancellationTokenSource? _loopCts;

    // Cancels the *currently playing* song only (Next)
    private CancellationTokenSource? _playCts;

    public async void Handle(EventType.Play @event)
    {
        try
        {
            _loopCts ??= new CancellationTokenSource();
            await foreach (var (ctx, callbacks) in channel.Reader.ReadAllAsync(_loopCts.Token))
            {
                using var currentPlayCts = CancellationTokenSource.CreateLinkedTokenSource(_loopCts.Token);
                _playCts = currentPlayCts;
                await playerService.Play(ctx, callbacks, currentPlayCts.Token);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in NetCordPlayerHandler");
        }
    }

    public void Handle(EventType.Stop @event)
    {
        Interlocked.Exchange(ref _loopCts, null)?.Cancel();
    }

    public void Handle(EventType.Skip @event)
    {
        Interlocked.Exchange(ref _playCts, null)?.Cancel();
    }
}