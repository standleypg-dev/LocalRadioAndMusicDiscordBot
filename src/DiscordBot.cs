using Discord;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot;

public class DiscordBot(DiscordSocketClient client,
    IInteractionService interactionService,
    Startup appStartup,
    IQueueService queueService,
    IConfiguration configuration) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        queueService.SongAdded += title =>
        {
            // Disable Spotify recommendation for now
            // _ = Task.Run(async () => { await spotifyService.GetRecommendationAsync(title); }, stoppingToken);
        };

        return RunBotAsync(stoppingToken);
    }
    private async Task RunBotAsync(CancellationToken stoppingToken)
    {
        await appStartup.SetupLoggingAndReadyEvents();
        await appStartup.SetupCommandHandling();

        client.InteractionCreated += interactionService.OnInteractionCreated;
        client.UserVoiceStateUpdated += interactionService.OnUserVoiceStateUpdated;

        await client.LoginAsync(TokenType.Bot, ConfigurationHelper.GetConfiguration<string>(configuration,"Discord:Token"));
        await client.StartAsync();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Task was cancelled, exit the loop
                break;
            }
        }
    }
}