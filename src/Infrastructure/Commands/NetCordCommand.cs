using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Domain.Events;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using NetCord.Services.ComponentInteractions;
using YoutubeExplode;
using YoutubeExplode.Common;
using Constants = Domain.Common.Constants;

namespace Infrastructure.Commands;

public class NetCordCommand(IServiceProvider serviceProvider, IMusicQueueService queue)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("play", "Play a track from Youtube or a radio station")]
    public async Task PingAsync([CommandParameter(Remainder = true)] string command)
    {
        if (await NotInVoiceChannel())
        {
            return;
        }

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
                message.Components =
                    CreateComponent(radiosSourceList.Select(rs => new ComponentModel(rs.Name, rs.Id.ToString())));
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

                message.Components =
                    CreateComponent(source.Select(s => new ComponentModel(s.Title, s.Url, s.Author.ChannelTitle)));
            }
                break;
        }

        await RespondAsync(InteractionCallback.Message(message));
    }

    [SlashCommand("stop", "Stop playing and clear the queue")]
    public async Task Stop()
    {
        if (await NotInVoiceChannel())
        {
            return;
        }

        DispatchEvent(new EventType.Stop());
        var message = CreateMessage<InteractionMessageProperties>("Stopping playback and clearing the queue.");
        await RespondAsync(InteractionCallback.Message(message));
    }

    [SlashCommand("skip", "Skip the current track")]
    public async Task Skip()
    {
        if (await NotInVoiceChannel())
        {
            return;
        }

        DispatchEvent(new EventType.Skip());
        var message = CreateMessage<InteractionMessageProperties>("Skipping the current track.");
        await RespondAsync(InteractionCallback.Message(message));
    }

    [SlashCommand("playlist", "Show the current playlist")]
    public async Task Playlist()
    {
        if (await NotInVoiceChannel())
        {
            return;
        }


        if (queue.Count == 0)
            await RespondAsync(InteractionCallback.Message("No songs in queue."));
        else
        {
            using var scope = serviceProvider.CreateScope();
            var youtubeService = scope.ServiceProvider.GetRequiredKeyedService<IStreamService>(nameof(YoutubeService));
            var songs = queue.GetAllRequests().Select(async r =>
                {
                    var title = await youtubeService.GetVideoTitleAsync(
                        (r.ContextAsObject as StringMenuInteractionContext)?.SelectedValues[0], CancellationToken.None);
                    return title;
                }
            ).ToList();
            
            var titles = await Task.WhenAll(songs);
            
            var response = "Queues: " + Environment.NewLine + string.Join(Environment.NewLine,
                titles.Select((title, index) =>
                {
                    var isPlayingNowMsg = index == 0 ? "(Playing now)" : "";
                    return $"{index + 1}. {title} {isPlayingNowMsg}";
                }));
            await RespondAsync(InteractionCallback.Message(response));
        }
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

    private async Task<bool> NotInVoiceChannel()
    {
        if (!Context.Guild!.VoiceStates.TryGetValue(Context.User.Id, out _))
        {
            var notInVoiceChannelMessage =
                CreateMessage<InteractionMessageProperties>("You must be in a voice channel to use this command.");
            await RespondAsync(InteractionCallback.Message(notInVoiceChannelMessage));
            return true;
        }

        return false;
    }

    private void DispatchEvent<TEvent>(TEvent @event) where TEvent : IEvent
    {
        using var scope = serviceProvider.CreateScope();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

        eventDispatcher.Dispatch(@event);
    }

    private record ComponentModel(string Title, string Url, string? Description = null);
}