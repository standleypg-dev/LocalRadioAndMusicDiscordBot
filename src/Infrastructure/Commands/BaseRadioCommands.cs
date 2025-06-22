using Application.Configs;
using Application.DTOs;
using Application.DTOs.Stats;
using Application.Interfaces.Commands;
using Application.Interfaces.Services;
using Application.Store;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Infrastructure.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Infrastructure.Commands;

public class BaseRadioCommands(
    IAudioPlayerService<SongDto<SocketVoiceChannel>, SocketVoiceChannel> audioPlayer,
    IJokeService jokeService,
    IQuoteService quoteService,
    IQueueService<SongDto<SocketVoiceChannel>> queueService,
    IServiceProvider serviceProvider,
    GlobalStore globalStore,
    ILogger<BaseRadioCommands> logger,
    IConfiguration configuration)
    : ModuleBase<SocketCommandContext>, IRadioCommand<string>
{
    [Summary("Plays a song or a radio station.")]
    public async Task PlayCommand([Remainder] string command)
    {
        if (!IsUserEligibleForCommand(Context.User))
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        if (command.Equals("radio"))
        {
            MessageComponentGenerator.GenerateComponents(
                configuration.GetConfiguration<List<RadioDto>>("Radios"),
                colInRow: 2);
            var embed = new EmbedBuilder()
                .WithTitle("Choose your favorite radio station:")
                .WithFooter("Powered by RMT & Astro")
                .Build();

            await ReplyAsync(embed: embed,
                components: MessageComponentGenerator.GenerateComponents(
                    configuration.GetConfiguration<List<RadioDto>>("Radios"), colInRow: 3));
        }
        else if (Uri.TryCreate(command, UriKind.Absolute, out _))
        {
            var videos =
                await youtubeClient.Search.GetVideosAsync(command)
                    .CollectAsync(5); // Adjust the number of results we want

            var embed = new EmbedBuilder()
                .WithTitle("Click to play or to add to the queue:")
                .Build();

            await ReplyAsync(embed: embed, components: MessageComponentGenerator.GenerateComponents(videos.ToList()));
        }
        else
        {
            var videos =
                await youtubeClient.Search.GetVideosAsync(command)
                    .CollectAsync(5); // Adjust the number of results we want

            var embed = new EmbedBuilder()
                .WithTitle("Choose your song")
                .Build();

            await ReplyAsync(embed: embed, components: MessageComponentGenerator.GenerateComponents(videos.ToList()));
        }
    }

    public async Task PlayFromPlaylistCommand([Remainder] string command)
    {
        if (!IsUserEligibleForCommand(Context.User))
        {
            return;
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();

            if (Uri.TryCreate(command, UriKind.Absolute, out var playlistUrl))
            {
                var playlist = await youtubeClient.Playlists.GetAsync(playlistUrl.ToString());
                var videos = await youtubeClient.Playlists.GetVideosAsync(playlist.Id);

                int idx = 1;
                foreach (var video in videos)
                {
                    var song = new SongDto<SocketVoiceChannel>
                    {
                        Url = $"{video.Url}&index={idx}",
                        Title = video.Title,
                        VoiceChannel = (Context.User as SocketGuildUser)?.VoiceChannel,
                        UserId = Context.User.Id
                    };

                    await queueService.AddSongAsync(song, followup: false);
                    idx++;
                }

                var embed = new EmbedBuilder()
                    .WithTitle($"Added {videos.Count} songs from playlist: {playlist.Title}")
                    .WithDescription(string.Join(Environment.NewLine, videos.Select(v => v.Title)))
                    .WithFooter("Powered by Not So Smart Music Bot")
                    .Build();

                await ReplyAsync(embed: embed);

                await audioPlayer.OnPlaylistChanged().ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("Invalid playlist URL.");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in PlayFromPlaylistCommand: {Message}", e.Message);
            throw;
        }
    }

    [Summary("Displays the help message for the bot commands.")]
    public async Task HelpCommand()
    {
        var helpMessage = configuration.GetConfiguration<HelpMessageDto>("HelpMessage");
        var embed = new EmbedBuilder()
            .WithTitle(helpMessage.Title)
            .WithDescription(helpMessage.Description)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Summary("Stops the currently playing song or radio station.")]
    public async Task StopCommand()
    {
        if (!IsUserEligibleForCommand(Context.User))
        {
            return;
        }

        await ReplyAsync("Stopping radio..");
        await audioPlayer.DestroyVoiceChannelAsync();
        await queueService.ClearQueueAsync();
    }

    [Summary("Skips to the next song in the queue.")]
    public async Task NextCommand()
    {
        if (!IsUserEligibleForCommand(Context.User))
        {
            return;
        }

        if ((await queueService.GetQueueAsync()).Count == 1)
            await ReplyAsync("No songs in queue. Nothing to next.");
        else
        {
            await ReplyAsync("Playing next song..");
            await audioPlayer.NextSongAsync();
        }
    }

    [Summary("Displays the current playlist.")]
    public async Task QueueCommand()
    {
        var songs = await queueService.GetQueueAsync();

        if (songs.Count == 0)
            await ReplyAsync("No songs in queue.");
        else
        {
            await ReplyAsync("Queues: " + Environment.NewLine + string.Join(Environment.NewLine,
                songs.Select((title, index) =>
                {
                    var isPlayingNowMsg = index == 0 ? "(Playing now)" : "";
                    return $"{index + 1}. {title.Title} {isPlayingNowMsg}";
                })));
        }
    }

    [Summary("Tells a random joke.")]
    public async Task TellJoke([Remainder] string command)
    {
        await ReplyAsync(await jokeService.GetJokeAsync(), isTTS: true);
    }

    [Summary("Tells a random motivational quote.")]
    public async Task TellQuote([Remainder] string command)
    {
        await ReplyAsync(await quoteService.GetQuoteAsync(), isTTS: true);
    }

    [Summary("Displays user statistics.")]
    public async Task UserStatsCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        using var scope = serviceProvider.CreateScope();
        var statisticsService = scope.ServiceProvider
            .GetRequiredService<IStatisticsService<SocketUser, SongDto<SocketVoiceChannel>>>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        UserStatsDto? userStats;
        List<TopSongDto> topSongs;
        List<RecentPlayDto> recentPlays;
        var embed = new EmbedBuilder();

        if (command.Equals("me") || command.Equals("mine"))
        {
            userStats = await statisticsService.GetUserStatsAsync(Context.User.Id);
            topSongs = await statisticsService.GetUserTopSongsAsync(Context.User.Id);
            recentPlays = await statisticsService.GetUserRecentPlaysAsync(Context.User.Id);

            await BuildUserStatsEmbed(true);
        }
        else if (command.Equals("all") || command.Equals("everyone"))
        {
            topSongs = await statisticsService.GetTopSongsAsync();
            embed.WithTitle("All Time Top Songs")
                .WithDescription(string.Join(Environment.NewLine,
                    topSongs.Select(ts => $"{ts.Title} - {ts.PlayCount} plays")));
        }
        else if (command.Equals("today"))
        {
            topSongs = await statisticsService.GetTopSongsAsync(isToday: true);
            embed.WithTitle("Today's Top Songs")
                .WithDescription(string.Join(Environment.NewLine,
                    topSongs.Select(ts => $"{ts.Title} - {ts.PlayCount} plays")));
        }
        else
        {
            // If the command is not recognized, we can try to find the user by username or display name
            var user = await userService.GetUserByUsernameAsync(command) ??
                       await userService.GetUserByDisplayNameAsync(command);

            if (user == null)
            {
                await ReplyAsync($"User '{command}' not found.");
                return;
            }

            userStats = await statisticsService.GetUserStatsAsync(user.Id);
            topSongs = await statisticsService.GetUserTopSongsAsync(user.Id);
            recentPlays = await statisticsService.GetUserRecentPlaysAsync(user.Id);

            await BuildUserStatsEmbed();
        }

        embed.WithFooter("Powered by Not So Smart Music Bot");
        await ReplyAsync(embed: embed.Build());

        async Task BuildUserStatsEmbed(bool self = false)
        {
            if (userStats == null || topSongs.Count == 0 || recentPlays.Count == 0)
            {
                await ReplyAsync("No stats found for this user.");
                return;
            }

            if (self)
            {
                embed.WithColor(Color.DarkOrange)
                    .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                    .WithAuthor(Context.User.Username,
                        Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());
            }

            embed.WithTitle($"Stats for {userStats.Username}")
                .AddField("Your first play", userStats.MemberSince.ToLocalTime().ToString("MM/dd/yyyy HH:mm"))
                .AddField("Total Plays", userStats.TotalPlays)
                .AddField("Unique Songs", userStats.UniqueSongs)
                .AddField("Top Songs",
                    string.Join(Environment.NewLine,
                        topSongs.Select((ts, index) => $"{index + 1}. {ts.Title} - {ts.PlayCount} plays")))
                .AddField("Recent Plays",
                    string.Join(Environment.NewLine,
                        recentPlays.Select((rp, index) =>
                            $"{index + 1}. {rp.Title} at {rp.PlayedAt.ToLocalTime():MM/dd/yyyy HH:mm}")));
        }
    }

    [Summary("Blacklists a song from the queue.")]
    public async Task BlacklistCommand([Remainder] string command)
    {
        if (!IsAdminUser(Context.User))
        {
            await ReplyAsync("You do not have permission to use this command.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

        if (command.Equals("this"))
        {
            if (!globalStore.TryGet<Queue<SongDto<SocketVoiceChannel>>>(out var songs))
            {
                await ReplyAsync("No songs in queue to blacklist.");
                return;
            }

            var song = songs.Peek();
            await blacklistService.AddToBlacklistAsync(song.Url);
            await ReplyAsync($"Blacklisted {song.Title} ({song.Url}) from the queue.");

            // if the the queue has only one song, stop it
            if (songs.Count == 1)
            {
                await StopCommand();
            }
            else
            {
                await NextCommand();
            }
        }
    }

    [Summary("Unblacklists a song from the queue.")]
    public async Task UnblacklistCommand([Remainder] string command)
    {
        if (!IsAdminUser(Context.User))
        {
            await ReplyAsync("You do not have permission to use this command.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

        if (command.Equals(command, StringComparison.OrdinalIgnoreCase))
        {
            await blacklistService.RemoveFromBlacklistAsync(command);
            await ReplyAsync(
                $"Unblacklisted ({command}) from the queue. This will take effect only if the song have been blacklisted before.");
        }
    }

    // get list of blacklisted songs
    [Summary("Lists all blacklisted songs.")]
    public async Task BlacklistListCommand()
    {
        using var scope = serviceProvider.CreateScope();
        var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

        var blacklistedSongs = await blacklistService.GetBlacklistedSongsAsync();

        if (blacklistedSongs.Count == 0)
        {
            await ReplyAsync("No songs are currently blacklisted.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Blacklisted Songs")
            .WithDescription(string.Join(Environment.NewLine, blacklistedSongs.Select(s => s.Title)))
            .Build();

        await ReplyAsync(embed: embed);
    }

    private bool IsUserEligibleForCommand(SocketUser user)
    {
        // check if user is in the voice channel
        if (user is not SocketGuildUser guildUser || guildUser.VoiceChannel == null)
        {
            ReplyAsync("You need to be in a voice channel to use this command.");
            return false;
        }

        // check if user is self deafened or deafened
        if (guildUser.IsSelfDeafened || guildUser.IsDeafened)
        {
            ReplyAsync("You cannot use this command while deafened.");
            return false;
        }

        return true;
    }

    private static bool IsAdminUser(SocketUser user)
    {
        // Check if the user is an admin (you can customize this logic)
        return user is SocketGuildUser { GuildPermissions.Administrator: true };
    }
}