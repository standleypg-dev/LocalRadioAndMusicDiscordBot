using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using radio_discord_bot;
using radio_discord_bot.Configs;
using radio_discord_bot.Services;


public class Program
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _client;
    private readonly IInteractionService _interactionService;
    private readonly Startup _appStartup;

    public static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

    public Program()
    {
        _serviceProvider = DependencyInjection.CreateProvider();
        _client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
        _interactionService = _serviceProvider.GetRequiredService<IInteractionService>();
        _appStartup = _serviceProvider.GetRequiredService<Startup>();
    }

    public async Task RunBotAsync()
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
