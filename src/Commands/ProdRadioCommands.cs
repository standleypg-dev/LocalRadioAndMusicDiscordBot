using Discord.Commands;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Commands;

public class ProdRadioCommands(
    IAudioPlayerService audioPlayer,
    IJokeService jokeService,
    IQuoteService quoteService,
    IQueueService queueService,
    IServiceProvider serviceProvider,
    IConfiguration configuration)
    : BaseRadioCommands(audioPlayer, jokeService, quoteService, queueService, serviceProvider, configuration),
        IRadioCommand
{
    [Command("play")]
    [Summary("Plays a song or a radio station.")]
    public new async Task PlayCommand([Remainder] string command) => await base.PlayCommand(command);

    [Command("help")]
    [Summary("Displays the help message for the bot commands.")]
    public new async Task HelpCommand() => await base.HelpCommand();

    [Command("stop")]
    [Summary("Stops the currently playing song or radio station.")]
    public new async Task StopCommand() => await base.StopCommand();

    [Command("next")]
    [Summary("Skips to the next song in the queue.")]
    public new async Task NextCommand() => await base.NextCommand();

    [Command("playlist")]
    [Summary("Displays the current playlist.")]
    public new async Task QueueCommand() => await base.QueueCommand();

    [Command("tell")]
    [Summary("Tells a random joke or quote.")]
    public new async Task TellJoke([Remainder] string command) => await base.TellJoke(command);

    [Command("motivate")]
    [Summary("Tells a random motivational quote.")]
    public new async Task TellQuote([Remainder] string command) => await base.TellQuote(command);

    [Command("stats")]
    public new async Task UserStatsCommand(string command) => await base.UserStatsCommand(command);
}