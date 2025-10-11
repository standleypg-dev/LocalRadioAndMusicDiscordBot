using Application.Configs;
using Application.Interfaces.Services;
using Application.Services;
using Application.Store;
using Domain.Common;
using Infrastructure.Commands;
using Infrastructure.Interaction;
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
using AssemblyMarker = Application.AssemblyMarker;

namespace Worker;

public static class DependencyInjection
{
    public static void AddDiscordServices(this IServiceCollection services, IConfiguration configuration)
    {
        var intents = GatewayIntents.Guilds |
                      GatewayIntents.GuildMessages |
                      GatewayIntents.GuildVoiceStates |
                      GatewayIntents.MessageContent |
                      GatewayIntents.DirectMessages;

        services.AddDiscordGateway(options =>
            {
                options.Token = configuration.GetConfiguration<string>("Discord:Token");
                options.Intents = intents;
            })
            .AddGatewayHandlers(typeof(Program).Assembly)
            .AddApplicationCommands()
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>();
            // .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>();

        // Assembly markers to locate assemblies for eventing
        // This is needed for the subscription to work correctly
        services.AddEventing(typeof(AssemblyMarker).Assembly,
            typeof(Infrastructure.Services.AssemblyMarker).Assembly);

        services.AddLogging(opts => opts.AddConsole());

        services.AddSingleton<GlobalStore>();
        services.AddSingleton<PlayerState<VoiceClient>>();
        services.AddSingleton<IHttpRequestService, HttpRequestService>();
        services.AddSingleton<INativePlaceMusicProcessorService, FfmpegProcessService>();
        services.AddSingleton<INetCordAudioPlayerService, NetCordAudioPlayerService>();
        services.AddSingleton<IMusicQueueService, MusicQueueService>();
        services.AddSingleton<IScopeExecutor, ScopeExecutor>();

        services.AddScoped<YoutubeClient>();
        services.AddScoped<SoundCloudClient>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IBlacklistService, BlacklistService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRadioSourceService, RadioSourceService>();

        services.AddKeyedScoped<IStreamService, YoutubeService>(nameof(YoutubeService));
        services.AddKeyedScoped<IStreamService, SoundCloudService>(nameof(SoundCloudService));
        services.AddKeyedScoped<IRandomService, JokeService>(nameof(JokeService));
        services.AddKeyedScoped<IRandomService, QuoteService>(nameof(QuoteService));

        services.AddTransient<ISpotifyService, SpotifyService>();
    }

    public static void AddWebApplication(this WebApplication app)
    {
        app.AddApplicationCommandModule<PlayCommand>()
            .AddApplicationCommandModule<MusicActionCommands>()
            .AddApplicationCommandModule<MiscCommands>()
            .AddApplicationCommandModule<AdminCommands>()
            .AddComponentInteractionModule<NetCordInteraction>();
    }
}