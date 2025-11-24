using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Domain.Events;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Commands;

public class MusicActionCommands(IScopeExecutor executor, IMusicQueueService queue)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("stop", "Stop playing and clear the queue")]
    public async Task Stop()
    {
        if (await CommandUtils.NotInVoiceChannel(Context, (message) => RespondAsync(message)))
        {
            return;
        }

        DispatchEvent(new EventType.Stop());
        var message =
            CommandUtils.CreateMessage<InteractionMessageProperties>("Stopping playback and clearing the queue.");
        await RespondAsync(InteractionCallback.Message(message));
    }

    [SlashCommand("skip", "Skip the current track")]
    public async Task Skip()
    {
        if (await CommandUtils.NotInVoiceChannel(Context, (message) => RespondAsync(message)))
        {
            return;
        }

        DispatchEvent(new EventType.Skip());
        var message = CommandUtils.CreateMessage<InteractionMessageProperties>("Skipping the current track.");
        await RespondAsync(InteractionCallback.Message(message));
    }

    [SlashCommand("playlist", "Show the current playlist")]
    public async Task Playlist()
    {
        if (await CommandUtils.NotInVoiceChannel(Context, (message) => RespondAsync(message)))
        {
            return;
        }

        if (queue.Count == 0)
            await RespondAsync(InteractionCallback.Message("No songs in queue."));
        else
        {
            await executor.ExecuteAsync(async serviceProvider =>
            {
                var youtubeService = serviceProvider.GetRequiredKeyedService<IStreamService>(nameof(YoutubeService));
                var songs = queue.GetAllRequests().Select(async r =>
                    {
                        var title = await youtubeService.GetVideoTitleAsync(
                            r.VideoUrl ?? (r.ContextAsObject as StringMenuInteractionContext)?.SelectedValues[0]!,
                            CancellationToken.None);
                        return title;
                    }
                ).Take(20).ToList();

                var titles = await Task.WhenAll(songs);

                var response = "Queues: " + Environment.NewLine + string.Join(Environment.NewLine,
                    titles.Select((title, index) =>
                    {
                        var isPlayingNowMsg = index == 0 ? "(Playing now)" : "";
                        return $"{index + 1}. {title} {isPlayingNowMsg}";
                    }));
                await RespondAsync(InteractionCallback.Message(response));
            });
        }
    }

    [SlashCommand("rewind", "Rewind the current track")]
    public async Task Rewind()
    {
        if (await CommandUtils.NotInVoiceChannel(Context, (message) => RespondAsync(message)))
        {
            return;
        }

        InteractionMessageProperties message;
        if(queue.Count == 0)
        {
            message = CommandUtils.CreateMessage<InteractionMessageProperties>("No songs in queue.");
            await RespondAsync(InteractionCallback.Message(message));
            return;
        }
        
        queue.Rewind();
        message = CommandUtils.CreateMessage<InteractionMessageProperties>("Rewinding the current track.");
        await RespondAsync(InteractionCallback.Message(message));
    }

    [SlashCommand("statistics", "Show some statistics")]
    public async Task Statistics()
    {
        var statisticWebsiteUrl = new Uri("https://rytho.standleypg.com/");
        var message =
            CommandUtils.CreateMessage<InteractionMessageProperties>(
                $"You can find the statistics of this bot at: {statisticWebsiteUrl}");
        await RespondAsync(InteractionCallback.Message(message));
    }

    private void DispatchEvent<TEvent>(TEvent @event) where TEvent : IEvent
    {
        executor.Execute(serviceProvider =>
        {
            using var scope = serviceProvider.CreateScope();
            var eventDispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

            eventDispatcher.Dispatch(@event);
        });
    }
}