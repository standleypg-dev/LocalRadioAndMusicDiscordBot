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
    public new async Task PlayCommand([Remainder] string command) => await base.PlayCommand(command);

    [Command("helpdev")]
    public new async Task HelpCommand() => await base.HelpCommand();

    [Command("stopdev")]
    public new async Task StopCommand() => await base.StopCommand();

    [Command("nextdev")]
    public new async Task NextCommand() => await base.NextCommand();

    [Command("playlistdev")]
    public new async Task QueueCommand() => await base.QueueCommand();

    [Command("telldev")]
    public new async Task TellJoke([Remainder] string command) => await base.TellJoke(command);

    [Command("motivatedev")]
    public new async Task TellQuote([Remainder] string command) => await base.TellQuote(command);

    [Command("statsdev")]
    public new async Task UserStatsCommand(string command) => await base.UserStatsCommand(command);
}