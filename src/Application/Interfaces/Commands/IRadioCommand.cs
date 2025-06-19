
namespace Application.Interfaces.Commands;

public interface IRadioCommand<in TCommand, TContext>
    where TContext : class
{
    Task PlayCommand(TCommand command);
    Task HelpCommand();
    Task StopCommand();
    Task NextCommand();
    Task QueueCommand();
    Task TellJoke(TCommand command);
    Task TellQuote(TCommand command);
    Task UserStatsCommand(TCommand command);
    Task BlacklistCommand(TCommand command);
    Task UnblacklistCommand(TCommand command);
    Task BlacklistListCommand();
}