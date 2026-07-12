using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using NetCord.Services.ComponentInteractions;

namespace Infrastructure.Commands;

[SlashCommand("action", "Blacklist a song from being played")]
[RequireUserPermissions<ApplicationCommandContext>(Permissions.Administrator)]
public class AdminCommands(
    [FromKeyedServices(nameof(YoutubeService))]
    IStreamService youtubeService,
    IMusicQueueService queue,
    IServiceProvider serviceProvider) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("blacklist", "Blacklist the currently playing song")]
    public async Task BlacklistAsync()
    {
        var song = queue.NowPlaying;
        if (song is null)
        {
            await RespondAsync(InteractionCallback.Message(
                "There is no song to blacklist. Please use the /play command to search for a song first."));
            return;
        }

        var url = song.VideoUrl ?? (song.ContextAsObject as StringMenuInteractionContext)?.SelectedValues[0];
        if (url is null)
        {
            await RespondAsync(InteractionCallback.Message("No song was selected to blacklist."));
            return;
        }

        if (Guid.TryParse(url, out _))
        {
            await RespondAsync(InteractionCallback.Message("Radio stations cannot be blacklisted."));
            return;
        }

        var title = await youtubeService.GetVideoTitleAsync(url, CancellationToken.None);

        using var scope = serviceProvider.CreateScope();
        var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

        var added = await blacklistService.AddToBlacklistAsync(url);
        if (!added)
        {
            await RespondAsync(InteractionCallback.Message(
                $"The song with title '{title}' was not found in the library and could not be blacklisted."));
            return;
        }

        await RespondAsync(InteractionCallback.Message($"The song with title '{title}' has been blacklisted."));

        // Skip the blacklisted track; the player disconnects on its own when the queue is empty.
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        eventDispatcher.Dispatch(new EventType.Skip());
    }

    [SubSlashCommand("unblacklist", "Remove a song from the blacklist")]
    public async Task UnblacklistAsync([CommandParameter(Remainder = true, Name = "song title")] string title)
    {
        using var scope = serviceProvider.CreateScope();
        var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

        var removed = await blacklistService.RemoveFromBlacklistAsync(title);

        var message =
            CommandUtils.CreateMessage<InteractionMessageProperties>(removed
                ? $"The song with title '{title}' has been removed from the blacklist."
                : $"No blacklisted song matching '{title}' was found.");
        await RespondAsync(InteractionCallback.Message(message));
    }

    [SubSlashCommand("list", "List all blacklisted songs")]
    public async Task ListBlacklistedSongsAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

        var blacklistedSongs = await blacklistService.GetBlacklistedSongsAsync();

        if (blacklistedSongs.Count == 0)
        {
            await RespondAsync(InteractionCallback.Message("There are no blacklisted songs."));
            return;
        }

        var message = string.Join(Environment.NewLine,
            blacklistedSongs.Select((song, index) => $"{index + 1}. {song.Title}"));

        await RespondAsync(InteractionCallback.Message(message));
    }
}
