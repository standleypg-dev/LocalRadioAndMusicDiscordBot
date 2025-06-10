using Discord.Commands;
using radio_discord_bot.Services.Interfaces;
using YoutubeExplode;

namespace radio_discord_bot.Commands;

public class ProdRadioCommands(
    IAudioPlayerService audioPlayer,
    IJokeService jokeService,
    IQuoteService quoteService,
    IQueueService queueService,
    IServiceProvider serviceProvider,
    IConfiguration configuration) : BaseRadioCommands(audioPlayer, jokeService, quoteService, queueService, serviceProvider, configuration)
{
    [Command("play")]
    [Summary("Plays a song or a radio station.")]
    public async Task PlayCommand([Remainder] string command) => await base.PlayCommand(command);

    [Command("help")]
    [Summary("Displays the help message for the bot commands.")]
    public async Task HelpCommand() => await base.HelpCommand();
    
    [Command("stop")]
    [Summary("Stops the currently playing song or radio station.")]
    public async Task StopCommand() => await base.StopCommand();
    
    [Command("next")]
    [Summary("Skips to the next song in the queue.")]
    public async Task NextCommand() => await base.NextCommand();
    
    [Command("playlist")]
    [Summary("Displays the current playlist.")]
    public async Task QueueCommand() => await base.QueueCommand();
    
    [Command("tell")]
    [Summary("Tells a random joke or quote.")]
    public async Task TellJoke([Remainder] string command) => await base.TellJoke(command);
    
    [Command("motivate")]
    [Summary("Tells a random motivational quote.")]
    public async Task TellQuote([Remainder] string command) => await base.TellQuote(command);
}