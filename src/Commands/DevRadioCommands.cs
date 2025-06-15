using radio_discord_bot.Services.Interfaces;
using YoutubeExplode;
using Discord.Commands;

namespace radio_discord_bot.Commands;

public class DevRadioCommands(
    IAudioPlayerService audioPlayer,
    IJokeService jokeService,
    IQuoteService quoteService,
    IQueueService queueService,
    IServiceProvider serviceProvider,
    IConfiguration configuration)
    : BaseRadioCommands(audioPlayer, jokeService, quoteService, queueService, serviceProvider, configuration),
        IRadioCommand
{
    [Command("playdev")]
    [Summary("Plays a song or a radio station.")]
    public new async Task PlayCommand([Remainder] string command) => await base.PlayCommand(command);

    [Command("helpdev")]
    [Summary("Displays the help message for the bot commands.")]
    public new async Task HelpCommand() => await base.HelpCommand();

    [Command("stopdev")]
    [Summary("Stops the currently playing song or radio station.")]
    public new async Task StopCommand() => await base.StopCommand();

    [Command("nextdev")]
    [Summary("Skips to the next song in the queue.")]
    public new async Task NextCommand() => await base.NextCommand();

    [Command("playlistdev")]
    [Summary("Displays the current playlist.")]
    public new async Task QueueCommand() => await base.QueueCommand();

    [Command("telldev")]
    [Summary("Tells a random joke or quote.")]
    public new async Task TellJoke([Remainder] string command) => await base.TellJoke(command);

    [Command("motivatedev")]
    [Summary("Tells a random motivational quote.")]
    public new async Task TellQuote([Remainder] string command) => await base.TellQuote(command);

    [Command("statsdev")]
    public new async Task UserStatsCommand(string command) => await base.UserStatsCommand(command);
}