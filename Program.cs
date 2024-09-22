using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using radio_discord_bot.Configs;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot;

public class Program
{
    private readonly DiscordSocketClient _client;
    private readonly IInteractionService _interactionService;
    private readonly Startup _appStartup;

    public static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

    private Program()
    {
        var serviceProvider = DependencyInjection.CreateProvider();
        _client = serviceProvider.GetRequiredService<DiscordSocketClient>();
        _interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        _appStartup = serviceProvider.GetRequiredService<Startup>();
    }

    private async Task RunBotAsync()
    {
        await _appStartup.SetupLoggingAndReadyEvents();
        await _appStartup.SetupCommandHandling();

        _client.InteractionCreated += _interactionService.OnInteractionCreated;
        _client.UserVoiceStateUpdated += _interactionService.OnUserVoiceStateUpdated;

        await _client.LoginAsync(TokenType.Bot, Configuration.GetConfiguration<string>("BotToken"));
        await _client.StartAsync();

        await Task.Delay(-1);
    }
}