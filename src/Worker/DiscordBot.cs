using Application.Configs;
using Application.DTOs;
using Application.Interfaces.Services;
using Application.Store;
using Discord;
using Discord.WebSocket;
using Infrastructure.Interfaces;
using Infrastructure.Interfaces.Services;
using Infrastructure.Utils;

namespace Worker;

public class DiscordBot(
    DiscordSocketClient client,
    IInteractionService interactionService,
    Startup appStartup,
    IQueueService<SongDto<SocketVoiceChannel>> queueService,
    // GlobalStore globalStore,
    IConfiguration configuration) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        queueService.SongAdded += async title =>
        {
            await Task.CompletedTask;
            // Disable Spotify recommendation for now
            // _ = Task.Run(async () =>
            // {
            //     await spotifyService.GetRecommendationAsync(title);
            //     var embed = new EmbedBuilder()
            //         .WithTitle("You might like these as well:")
            //         .Build();
            //     await ReplyToChannel.FollowupEmbebAsync(globalStore.Get<SocketMessageComponent>()!,
            //         MessageComponentGenerator.GenerateComponents(response.Tracks.ToList(), 2), embed);
            // }, stoppingToken);
        };

        return RunBotAsync(stoppingToken);
    }

    private async Task RunBotAsync(CancellationToken stoppingToken)
    {
        await appStartup.SetupLoggingAndReadyEvents();
        await appStartup.SetupCommandHandling();

        client.InteractionCreated += interactionService.OnInteractionCreated;
        client.UserVoiceStateUpdated += interactionService.OnUserVoiceStateUpdated;

        await client.LoginAsync(TokenType.Bot,
            configuration.GetConfiguration<string>("Discord:Token"));
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