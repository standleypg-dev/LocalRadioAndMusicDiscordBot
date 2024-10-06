using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using radio_discord_bot.Commands;
using radio_discord_bot.Services;
using radio_discord_bot.Services.Implementations;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using YoutubeExplode;

namespace radio_discord_bot;

public static class DependencyInjection
{
    public static IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates |
                             GatewayIntents.GuildMembers | GatewayIntents.MessageContent | GatewayIntents.DirectMessages
        };

        var collection = new ServiceCollection()
            .AddLogging(opts => opts.AddConsole())
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<CommandService>()
            .AddSingleton<Startup>()
            .AddSingleton<RadioCommand>()
            .AddSingleton<GlobalStore>()
            .AddSingleton<IJokeService, JokeService>()
            .AddSingleton<IQuoteService, QuoteService>()
            .AddSingleton<IHttpRequestService, HttpRequestService>()
            .AddSingleton<IInteractionService, InteractionService>()
            .AddSingleton<IQueueService, QueueService>()
            .AddScoped<YoutubeClient>()
            .AddScoped<IYoutubeService, YoutubeService>()
            .AddSingleton<IFfmpegProcessService, FfmpegProcessService>()
            .AddSingleton<IAudioPlayerService, AudioPlayerService>()
            .AddTransient<IAudioService, AudioService>();

        return collection.BuildServiceProvider();
    }
}