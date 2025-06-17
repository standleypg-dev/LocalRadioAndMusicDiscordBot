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
    public new async Task PlayCommand([Remainder] string command) => await base.PlayCommand(command);

    [Command("help")]
    public new async Task HelpCommand() => await base.HelpCommand();

    [Command("stop")]
    public new async Task StopCommand() => await base.StopCommand();

    [Command("next")]
    public new async Task NextCommand() => await base.NextCommand();

    [Command("playlist")]
    public new async Task QueueCommand() => await base.QueueCommand();

    [Command("tell")]
    public new async Task TellJoke([Remainder] string command) => await base.TellJoke(command);

    [Command("motivate")]
    public new async Task TellQuote([Remainder] string command) => await base.TellQuote(command);

    [Command("stats")]
    public new async Task UserStatsCommand(string command) => await base.UserStatsCommand(command);
}