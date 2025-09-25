using System.Threading.Channels;
using AngleSharp.Common;
using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using static System.Text.Json.JsonSerializer;

namespace Infrastructure.Interaction;

public class NetCordInteraction(
    ILogger<NetCordInteraction> logger,
    IEventDispatcher eventDispatcher,
    Channel<PlayRequest<StringMenuInteractionContext>> channel) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction(Constants.CustomIds.Play)]
    public async Task<string> Play()
    {
        logger.LogInformation("Play command invoked by user {UserId} in guild {GuildId}", Context.User.Id,
            Context.Guild?.Id);

        await channel.Writer.WriteAsync(new PlayRequest<StringMenuInteractionContext>(Context, (Func<Task<InteractionCallbackResponse?>>?)NotInVoiceChannelCallback));
        eventDispatcher.Dispatch(new EventType.Play());

        return $"Added {Context.SelectedValues[0]} to the queue!";

        Task<InteractionCallbackResponse?> NotInVoiceChannelCallback() => RespondAsync(InteractionCallback.Message("You are not connected to any voice channel!"));
    }
    
    [ComponentInteraction(Constants.CustomIds.Skip)]
    public string Skip()
    {
        eventDispatcher.Dispatch(new EventType.Skip());
        
        return "Skip Requested";
    }
}