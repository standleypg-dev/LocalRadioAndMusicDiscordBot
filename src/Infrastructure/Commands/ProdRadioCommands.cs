using Application.DTOs;
using Application.Interfaces.Commands;
using Application.Interfaces.Services;
using Application.Store;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Commands;

public class ProdRadioCommands(
    IAudioPlayerService<SocketVoiceChannel> audioPlayer,
    IJokeService jokeService,
    IQuoteService quoteService,
    IQueueService<SongDto<SocketVoiceChannel>> queueService,
    IServiceProvider serviceProvider,
    GlobalStore globalStore,
    IConfiguration configuration)
    : BaseRadioCommands(audioPlayer, jokeService, quoteService, queueService, serviceProvider, globalStore, configuration),
        IRadioCommand<string>
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
    
    [Command("blacklist")]
    public new async Task BlacklistCommand([Remainder] string command) => await base.BlacklistCommand(command);
    [Command("unblacklist")]
    public new async Task UnblacklistCommand([Remainder] string command) => await base.UnblacklistCommand(command);
    [Command("blacklistedsong")]
    public new async Task BlacklistListCommand() => await base.BlacklistListCommand();
}