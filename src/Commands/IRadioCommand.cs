using Discord.Commands;

namespace radio_discord_bot.Commands;

public interface IRadioCommand
{
    Task PlayCommand([Remainder] string command);
    Task HelpCommand();
    Task StopCommand();
    Task NextCommand();
    Task QueueCommand();
    Task TellJoke([Remainder] string command);
    Task TellQuote([Remainder] string command);
    Task UserStatsCommand([Remainder] string command);
    Task BlacklistCommand([Remainder] string command);
    Task UnblacklistCommand([Remainder] string command);
    Task BlacklistListCommand();
}