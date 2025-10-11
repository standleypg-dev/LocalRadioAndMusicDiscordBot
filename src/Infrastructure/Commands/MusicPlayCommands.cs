using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Infrastructure.Commands;

[SlashCommand("play", "Play a track from Youtube or a radio station")]
public class PlayCommand(IScopeExecutor executor) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("music", "Play a track from Youtube")]
    public async Task MusicPlayer([CommandParameter(Remainder = true, Name = "song title")] string command)
    {
        if (await CommandUtils.NotInVoiceChannel(Context, (message) => RespondAsync(message)))
        {
            return;
        }

        await executor.ExecuteAsync(async serviceProvider =>
        {
            var blacklistService = serviceProvider.GetRequiredService<IBlacklistService>();
            if (await blacklistService.IsBlacklistedAsync(command))
            {
                var blacklistMessage =
                    CommandUtils.CreateMessage<InteractionMessageProperties>(
                        "The requested song is blacklisted and cannot be played.");
                await RespondAsync(InteractionCallback.Message(blacklistMessage));
            }
            else
            {
                var youtubeClient = serviceProvider.GetRequiredService<YoutubeClient>();
                var message = CommandUtils.CreateMessage<InteractionMessageProperties>("Select a track to play:");

                var source =
                    await youtubeClient.Search.GetVideosAsync(command)
                        .CollectAsync(5);

                message.Components =
                    CommandUtils.CreateButtonComponent(source.Select(s =>
                        new CommandUtils.ComponentModel(s.Title, s.Url, s.Author.ChannelTitle)));

                await RespondAsync(InteractionCallback.Message(message));
            }
        });
    }

    [SubSlashCommand("radio", "Play a radio station")]
    public async Task RadioPlayer()
    {
        if (await CommandUtils.NotInVoiceChannel(Context, (message) => RespondAsync(message)))
        {
            return;
        }

        await executor.ExecuteAsync(async serviceProvider =>
        {
            var radioSourceService = serviceProvider.GetRequiredService<IRadioSourceService>();
            var message =
                CommandUtils.CreateMessage<InteractionMessageProperties>("Select a radio station to play:");

            var radiosSourceList = (await radioSourceService.GetAllRadioSourcesAsync()).Where(rs => rs.IsActive);
            message.Components =
                CommandUtils.CreateStringMenuComponent(radiosSourceList.Select(rs =>
                    new CommandUtils.ComponentModel(rs.Name, rs.Id.ToString())));

            await RespondAsync(InteractionCallback.Message(message));
        });
    }

    [SubSlashCommand("playlist", "Play a track from a Youtube playlist URL")]
    public async Task PlayFromPlaylist([CommandParameter(Remainder = true, Name = "playlist url")] string playlistUrl)
    {
        if (await CommandUtils.NotInVoiceChannel(Context, (message) => RespondAsync(message)))
        {
            return;
        }

        await executor.ExecuteAsync(async serviceProvider =>
        {
            var youtubeClient = serviceProvider.GetRequiredService<YoutubeClient>();
            var message = CommandUtils.CreateMessage<InteractionMessageProperties>("Select a track to play:");

            try
            {
                var playlist = await youtubeClient.Playlists.GetAsync(playlistUrl);
                var videos = await youtubeClient.Playlists.GetVideosAsync(playlist.Id);

                message.Components =
                    CommandUtils.CreateStringMenuComponent(videos.Select(s =>
                        new CommandUtils.ComponentModel(s.Title, s.Url, s.Author.ChannelTitle)));

                await RespondAsync(InteractionCallback.Message(message));
            }
            catch (Exception)
            {
                var errorMessage =
                    CommandUtils.CreateMessage<InteractionMessageProperties>(
                        "Failed to retrieve the playlist. Please ensure the URL is correct.");
                await RespondAsync(InteractionCallback.Message(errorMessage));
            }
        });
    }
}