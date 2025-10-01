using Application.Interfaces.Services;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Infrastructure.Commands;

[SlashCommand("random", "Telling random jokes, quotes, and more")]
public class MiscCommands(
    [FromKeyedServices(nameof(JokeService))]
    IRandomService jokeService,
    [FromKeyedServices(nameof(QuoteService))]
    IRandomService quoteService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("joke", "Tell a random joke")]
    public async Task TellJokeAsync()
    {
        var result = await jokeService.GetAsync();

        var message = CommandUtils.CreateMessage<InteractionMessageProperties>(result);
        message.Tts = true;
        await RespondAsync(InteractionCallback.Message(message));
    }

    [SubSlashCommand("quote", "Tell a random quote")]
    public async Task TellQuoteAsync()
    {
        var result = await quoteService.GetAsync();
        var message = CommandUtils.CreateMessage<InteractionMessageProperties>(result);
        message.Tts = true;
        await RespondAsync(InteractionCallback.Message(message));
    }
}