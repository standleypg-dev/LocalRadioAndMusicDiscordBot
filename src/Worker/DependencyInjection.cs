using System.Threading.Channels;
using Application.Configs;
using Application.DTOs;
using Application.Interfaces.Services;
using Application.Services;
using Application.Store;
using Discord.Commands;
using Discord.WebSocket;
using Domain.Common;
using Infrastructure.Commands;
using Infrastructure.Interaction;
using Infrastructure.Interfaces.Services;
using Infrastructure.Services;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using SoundCloudExplode;
using YoutubeExplode;
using Channel = System.Threading.Channels.Channel;
using GatewayIntents = Discord.GatewayIntents;

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
        services
            .AddSingleton<IAudioPlayerService<SongDto<SocketVoiceChannel>, SocketVoiceChannel>, AudioPlayerService>();
        services.AddSingleton<DiscordBot>();
        services.AddSingleton<INetCordAudioPlayerService, NetCordAudioPlayerService>();

        services.AddScoped<YoutubeClient>();
        services.AddScoped<SoundCloudClient>();
        services.AddKeyedScoped<IStreamService, YoutubeService>(nameof(YoutubeService));
        services.AddKeyedScoped<IStreamService, SoundCloudService>(nameof(SoundCloudService));
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IBlacklistService, BlacklistService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRadioSourceService, RadioSourceService>();

        services.AddTransient<ISpotifyService, SpotifyService>();
    }

    public static void AddNetCordServices(this IServiceCollection services, IConfiguration configuration)
    {
        var intents = NetCord.Gateway.GatewayIntents.Guilds |
                      NetCord.Gateway.GatewayIntents.GuildMessages |
                      NetCord.Gateway.GatewayIntents.GuildVoiceStates |
                      NetCord.Gateway.GatewayIntents.MessageContent |
                      NetCord.Gateway.GatewayIntents.DirectMessages;

        services.AddDiscordGateway(options =>
            {
                options.Token = configuration.GetConfiguration<string>("Discord:Token");
                options.Intents = intents;
            })
            .AddGatewayHandlers(typeof(Program).Assembly)
            .AddApplicationCommands()
            .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>();

        // Assembly markers to locate assemblies for eventing
        // This is needed for the subscription to work correctly
        services.AddEventing(typeof(Application.AssemblyMarker).Assembly,
            typeof(Infrastructure.Services.AssemblyMarker).Assembly);

        services.AddSingleton(_ =>
        {
            var options = new BoundedChannelOptions(capacity: 100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            return Channel.CreateBounded<PlayRequest<StringMenuInteractionContext>>(options);
        });
        
        services.AddSingleton<PlayerState<VoiceClient>>();
        services.AddSingleton<IMusicQueueService, MusicQueueService>();
    }

    public static void AddNetCordWebApplication(this WebApplication app)
    {
        app.AddApplicationCommandModule<NetCordCommand>()
            .AddComponentInteractionModule<NetCordInteraction>();
    }
}