using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services;
using Application.Store;
using ApplicationDto.DTOs;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Infrastructure.Commands;
using Infrastructure.Interfaces;
using Infrastructure.Interfaces.Services;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoutubeExplode;

namespace Worker;

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
        services.AddSingleton<IQueueService<SongDto<SocketVoiceChannel>>, QueueService>();
        services.AddSingleton<INativePlaceMusicProcessorService, FfmpegProcessService>();
        services.AddSingleton<IAudioPlayerService<SocketVoiceChannel>, AudioPlayerService>();
        services.AddSingleton<DiscordBot>();
        
        services.AddScoped<YoutubeClient>();
        services.AddScoped<IYoutubeService, YoutubeService>();
        services.AddScoped<IStatisticsService<SocketUser, SongDto<SocketVoiceChannel>>, StatisticsService>();
        services.AddScoped<IBlacklistService, BlacklistService>();
        services.AddScoped<IUserService, UserService>();
        
        services.AddTransient<ISpotifyService, SpotifyService>();
    }
}