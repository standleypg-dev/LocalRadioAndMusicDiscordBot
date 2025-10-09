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
        if (!CheckMessageExpiration())
        {
            return "This interaction has expired. Please use the play command again.";
        }
        
        if (!NotInVoiceChannel())
        {
            return "You must be in a voice channel to use this command.";
        }
        
        if (!NotDeafened())
        {
            return "You must be deafened to use this command.";
        }
        
        logger.LogInformation("Play command invoked by user {UserId} in guild {GuildId}", Context.User.Id,
            Context.Guild?.Id);

        var playRequest = new PlayRequest<StringMenuInteractionContext>
        {
            Context = Context,
            Callbacks = async message => await RespondAsyncCallback(message),
        };
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

    }
    
    private bool NotInVoiceChannel()
    {
        return Context.Guild!.VoiceStates.TryGetValue(Context.User.Id, out _);
    }
    
    private bool NotDeafened()
    {
        var voiceState = Context.Guild!.VoiceStates[Context.User.Id];
        return !(voiceState.IsDeafened || voiceState.IsSelfDeafened);
    }
    
    private bool CheckMessageExpiration()
    {
        var messageCreationTime = Context.Message.CreatedAt;
        var interactionTime = Context.Interaction.CreatedAt;
        var timeDifference = interactionTime - messageCreationTime;
        return timeDifference.TotalDays <= 2;
    }

    private Task<InteractionCallbackResponse> RespondAsyncCallback(string message) => RespondAsync(InteractionCallback.Message(message))!;
}