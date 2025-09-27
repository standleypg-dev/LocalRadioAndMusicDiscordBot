using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using YoutubeExplode;
using YoutubeExplode.Common;
using Constants = Domain.Common.Constants;

namespace Infrastructure.Commands;

public class NetCordCommand(IServiceProvider serviceProvider, IConfiguration configuration): ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("play", "Play a track from SoundCloud")]
    public async Task PingAsync([CommandParameter(Remainder = true)] string command)
    {
        // await soundCloudClient.InitializeAsync();
        // var source =
        //     await soundCloudClient.Search.GetTracksAsync(command)
        //         .CollectAsync(6); 
        
        using var scope = serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        var source =
            await youtubeClient.Search.GetVideosAsync(command)
                .CollectAsync(5); 
        
        var message = CreateMessage<InteractionMessageProperties>("Select a track to play:");
        message.Components =
        [
            new StringMenuProperties(Constants.CustomIds.Play)
            {
                Options = source.Select(s => new StringMenuSelectOptionProperties(s.Title!, s.Url!)
                {
                    Description = s.Author.ChannelTitle
                }).ToList()
            }
        ];
        await RespondAsync(InteractionCallback.Message(message));
    }
    
    [SlashCommand("stop", "Stop playing and clear the queue")]
    public async Task Stop()
    {
        using var scope = serviceProvider.CreateScope();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        eventDispatcher.Dispatch(new EventType.Stop());
        var message = CreateMessage<InteractionMessageProperties>("Stopping playback and clearing the queue.");
        await RespondAsync(InteractionCallback.Message(message));
    }
    
    [SlashCommand("skip", "Skip the current track")]
    public async Task Skip()
    {
        using var scope = serviceProvider.CreateScope();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        eventDispatcher.Dispatch(new EventType.Skip());
        var message = CreateMessage<InteractionMessageProperties>("Skipping the current track.");
        await RespondAsync(InteractionCallback.Message(message));
    }
    
    static T CreateMessage<T>(string message) where T : IMessageProperties, new()
    {
        return new ()
        {
            Content = message,
            Components = [],
        };
    }
}