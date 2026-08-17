using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Common;
using Microsoft.Extensions.Logging;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Infrastructure.Interaction;

public class NetCordInteraction(
    ILogger<NetCordInteraction> logger,
    IGuildMusicService guildMusicService,
    IBlacklistService blacklistService,
    YoutubeClient youtubeClient) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction(Constants.CustomIds.Play)]
    public async Task<string> Play()
    {
        var error = EvaluateChecking();
        if (error != null)
        {
            return error;
        }

        logger.LogInformation("Play command invoked by user {UserId} in guild {GuildId}", Context.User.Id,
            Context.Guild?.Id);

        var selectedValue = Context.SelectedValues[0];

        if (!Guid.TryParse(selectedValue, out _) && await blacklistService.IsBlacklistedAsync(selectedValue))
        {
            return "The requested song is blacklisted and cannot be played.";
        }

        const string message = "Added requested track to the queue!";

        var playRequest = new PlayRequest<StringMenuInteractionContext>
        {
            Context = Context,
            Callbacks = FollowupAsyncCallback,
        };
        guildMusicService.Enqueue(Context.Guild!.Id, playRequest);

        return message;
    }

    [ComponentInteraction(Constants.CustomIds.PlayListPlay)]
    public async Task<string> PlayPlaylist()
    {
        var error = EvaluateChecking();
        if (error != null)
        {
            return error;
        }

        logger.LogInformation("Play command invoked by user {UserId} in guild {GuildId}", Context.User.Id,
            Context.Guild?.Id);

        var videos = await youtubeClient.Playlists.GetVideosAsync(Context.SelectedValues[0]);

        var blacklistedUrls = (await blacklistService.GetBlacklistedSongsAsync())
            .Select(s => s.SourceUrl)
            .ToHashSet();

        var added = 0;
        foreach (var video in videos)
        {
            if (blacklistedUrls.Contains(video.Url))
            {
                continue;
            }

            var playRequest = new PlayRequest<StringMenuInteractionContext>
            {
                Context = Context,
                Callbacks = FollowupAsyncCallback,
                VideoTitle = video.Title,
                VideoUrl = video.Url
            };
            guildMusicService.Enqueue(Context.Guild!.Id, playRequest);
            added++;
        }

        return added > 0
            ? "Playlist Added to the queue!"
            : "All songs in the playlist are blacklisted; nothing was added.";
    }

    private string? EvaluateChecking()
    {
        if (Context.Guild is null)
        {
            return "This command can only be used in a server.";
        }

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
            return "You must not be deafened to use this command.";
        }

        return null;
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

    // The initial interaction response is already consumed by the string returned from
    // Play()/PlayPlaylist(); later notifications (e.g. retry failures, disconnects) must
    // go through a follow-up message or Discord rejects them as "already acknowledged".
    private Task FollowupAsyncCallback(string message) => FollowupAsync(new InteractionMessageProperties { Content = message });
}
