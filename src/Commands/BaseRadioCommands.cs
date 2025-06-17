using Discord;
using Discord.Commands;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Models.Stats;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Utils;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace radio_discord_bot.Commands;

public class BaseRadioCommands(
    IAudioPlayerService audioPlayer,
    IJokeService jokeService,
    IQuoteService quoteService,
    IQueueService queueService,
    IServiceProvider serviceProvider,
    IConfiguration configuration)
    : ModuleBase<SocketCommandContext>, IRadioCommand
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
                ConfigurationHelper.GetConfiguration<List<Radio>>(configuration, "Radios"),
                colInRow: 2);
            var embed = new EmbedBuilder()
                .WithTitle("Choose your favorite radio station:")
                .WithFooter("Powered by RMT & Astro")
                .Build();

            await ReplyAsync(embed: embed,
                components: MessageComponentGenerator.GenerateComponents(
                    ConfigurationHelper.GetConfiguration<List<Radio>>(configuration, "Radios"), colInRow: 3));
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

    [Summary("Displays the help message for the bot commands.")]
    public async Task HelpCommand()
    {
        var helpMessage = ConfigurationHelper.GetConfiguration<HelpMessage>(configuration, "HelpMessage");
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
        using var scope = serviceProvider.CreateScope();
        var jokeService = scope.ServiceProvider.GetRequiredService<IJokeService>();
        if (command.Equals("joke"))
        {
            await ReplyAsync(await jokeService.GetJokeAsync(), isTTS: true);
        }
    }
    
    [Summary("Tells a random motivational quote.")]
    public async Task TellQuote([Remainder] string command)
    {
        using var scope = serviceProvider.CreateScope();
        var quoteService = scope.ServiceProvider.GetRequiredService<IQuoteService>();
        if (command.Equals("me"))
        {
            await ReplyAsync(await quoteService.GetQuoteAsync(), isTTS: true);
        }
    }

    [Summary("Displays user statistics.")]
    public async Task UserStatsCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        using var scope = serviceProvider.CreateScope();
        var statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        UserStats? userStats;
        List<TopSong> topSongs;
        List<RecentPlay> recentPlays;
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
                .AddField("Your first play", userStats.MemberSince.ToLocalTime().ToString("g"))
                .AddField("Total Plays", userStats.TotalPlays)
                .AddField("Unique Songs", userStats.UniqueSongs)
                .AddField("Top Songs",
                    string.Join(Environment.NewLine, topSongs.Select(ts => $"{ts.Title} - {ts.PlayCount} plays")))
                .AddField("Recent Plays",
                    string.Join(Environment.NewLine,
                        recentPlays.Select(rp => $"{rp.Title} at {rp.PlayedAt.ToLocalTime():g}")));
        }
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
}