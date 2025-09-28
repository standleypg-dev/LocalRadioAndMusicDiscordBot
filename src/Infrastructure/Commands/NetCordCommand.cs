using System.ComponentModel;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Eventing;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using Constants = Domain.Common.Constants;

namespace Infrastructure.Commands;

public class NetCordCommand(IServiceProvider serviceProvider, IConfiguration configuration)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("play", "Play a track from SoundCloud")]
    [Description("Either song title, youtube URL or 'radio'")]
    public async Task PingAsync([CommandParameter(Remainder = true)] string command)
    {
        // `AddApplicationCommands()` registers services as singleton,
        // so scope is needed to resolve scoped services.
        using var scope = serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        var radioSourceService = scope.ServiceProvider.GetRequiredService<IRadioSourceService>();

        var message = CreateMessage<InteractionMessageProperties>("Select a track to play:");

        switch (command)
        {
            case null:
            case "radio":
            case "Radio":
            {
                var radiosSourceList = (await radioSourceService.GetAllRadioSourcesAsync()).Where(rs => rs.IsActive);
                message.Components = CreateComponent(radiosSourceList.Select(rs => new ComponentModel(rs.Name, rs.Id.ToString())));
                break;
            }
            case var _ when Uri.TryCreate(command, UriKind.Absolute, out _):
            {
                // TODO: play the URL directly
                break;
            }
            default:
            {
                var source =
                    await youtubeClient.Search.GetVideosAsync(command)
                        .CollectAsync(5);

                message.Components = CreateComponent(source.Select(s => new ComponentModel(s.Title, s.Url, s.Author.ChannelTitle)));
            }
                break;
        }
        
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

    private static T CreateMessage<T>(string message) where T : IMessageProperties, new()
    {
        return new()
        {
            Content = message,
            Components = [],
        };
    }

    private static IEnumerable<IMessageComponentProperties> CreateComponent<T>(T source)
        where T : IEnumerable<ComponentModel>
    {
        return
        [
            new StringMenuProperties(Constants.CustomIds.Play)
            {
                Options = source.Select(s => new StringMenuSelectOptionProperties(s.Title, s.Url)
                {
                    Description = s.Description ?? string.Empty,
                }).ToList()
            }
        ];
    }

    private record ComponentModel(string Title, string Url, string? Description = null);
}