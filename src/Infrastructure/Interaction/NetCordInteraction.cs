using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Interaction;

public class NetCordInteraction(
    ILogger<NetCordInteraction> logger,
    IEventDispatcher eventDispatcher,
    IMusicQueueService queueService,
    [FromKeyedServices(nameof(YoutubeService))] IStreamService youtubeService) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction(Constants.CustomIds.Play)]
    public async Task<string> Play()
    {
        logger.LogInformation("Play command invoked by user {UserId} in guild {GuildId}", Context.User.Id,
            Context.Guild?.Id);

        var playRequest = new PlayRequest<StringMenuInteractionContext>(Context, NotInVoiceChannelCallback);
        queueService.Enqueue(playRequest);
        eventDispatcher.Dispatch(new EventType.Play());
        var selectedValue = Context.SelectedValues[0];

        string message;
        if (!Guid.TryParse(selectedValue, out _))
        {
            var title = await youtubeService.GetVideoTitleAsync(Context.SelectedValues[0], CancellationToken.None);
            message = $"Added {title} to the queue!";
        }
        else
        {
            message = $"Added radio source to the queue!";
        }
        
        return message;

        Task<InteractionCallbackResponse> NotInVoiceChannelCallback() => RespondAsync(InteractionCallback.Message("You are not connected to any voice channel!"))!;
    }
}