using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using radio_discord_bot.Commands;
using radio_discord_bot.Services;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using YoutubeExplode;

namespace radio_discord_bot;

public static class DependencyInjection
{
    public static void AddDiscordServices(this IServiceCollection services)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates |
                             GatewayIntents.GuildMembers | GatewayIntents.MessageContent | GatewayIntents.DirectMessages
        };

        services.AddLogging(opts => opts.AddConsole());
        
        services.AddSingleton(config);
        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton<CommandService>();
        services.AddSingleton<Startup>();
        services.AddSingleton<ProdRadioCommands>();
        services.AddSingleton<DevRadioCommands>();
        services.AddSingleton<GlobalStore>();
        services.AddSingleton<IJokeService, JokeService>();
        services.AddSingleton<IQuoteService, QuoteService>();
        services.AddSingleton<IHttpRequestService, HttpRequestService>();
        services.AddSingleton<IInteractionService, InteractionService>();
        services.AddSingleton<IQueueService, QueueService>();
        services.AddSingleton<IFfmpegProcessService, FfmpegProcessService>();
        services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
        services.AddSingleton<DiscordBot>();
        
        services.AddScoped<YoutubeClient>();
        services.AddScoped<IYoutubeService, YoutubeService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IBlacklistService, BlacklistService>();
        services.AddScoped<IUserService, UserService>();
        
        services.AddTransient<ISpotifyService, SpotifyService>();
    }
}