using Application.DTOs;
using Application.Interfaces.Commands;
using Application.Interfaces.Services;
using Application.Store;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Commands;

public class DevRadioCommands(
    IAudioPlayerService<SongDto<SocketVoiceChannel>, SocketVoiceChannel> audioPlayer,
    IJokeService jokeService,
    IQuoteService quoteService,
    IQueueService<SongDto<SocketVoiceChannel>> queueService,
    IServiceProvider serviceProvider,
    GlobalStore globalStore,
    ILogger<BaseRadioCommands> logger,
    IConfiguration configuration)
    : BaseRadioCommands(audioPlayer, jokeService, quoteService, queueService, serviceProvider, globalStore, logger,
            configuration),
        IRadioCommand<string>
{
    [Command("playdev")]
    public new async Task PlayCommand([Remainder] string command) => await base.PlayCommand(command);

    [Command("playfromdev")]
    public new async Task PlayFromPlaylistCommand([Remainder] string command) =>
        await base.PlayFromPlaylistCommand(command);

    [Command("helpdev")]
    public new async Task HelpCommand() => await base.HelpCommand();

    [Command("stopdev")]
    public new async Task StopCommand() => await base.StopCommand();

    [Command("nextdev")]
    public new async Task NextCommand() => await base.NextCommand();

    [Command("playlistdev")]
    public new async Task QueueCommand() => await base.QueueCommand();

    [Command("jokedev")]
    public new async Task TellJoke([Remainder] string command) => await base.TellJoke(command);

    [Command("quotedev")]
    public new async Task TellQuote([Remainder] string command) => await base.TellQuote(command);

    [Command("statsdev")]
    public new async Task UserStatsCommand(string command) => await base.UserStatsCommand(command);

    [Command("blockdev")]
    public new async Task BlacklistCommand([Remainder] string command) => await base.BlacklistCommand(command);

    [Command("unblockdev")]
    public new async Task UnblacklistCommand([Remainder] string command) => await base.UnblacklistCommand(command);

    [Command("blockeddev")]
    public new async Task BlacklistListCommand() => await base.BlacklistListCommand();
}