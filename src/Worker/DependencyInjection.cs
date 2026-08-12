using Application.Configs;
using Application.Eventing;
using Application.Interfaces.Services;
using Application.Services;
using Application.Store;
using Infrastructure.Commands;
using Infrastructure.Interaction;
using Infrastructure.Services;
using NetCord;
using NetCord.Gateway;
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
            .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>();

        // Assembly markers to locate assemblies for eventing
        // This is needed for the subscription to work correctly
        services.AddEventing(typeof(AssemblyMarker).Assembly,
            typeof(Infrastructure.Services.AssemblyMarker).Assembly);

        services.AddLogging(opts => opts.AddConsole());

        services.AddHttpClient();

        services.AddSingleton<GlobalStore>();
        services.AddSingleton<IHttpRequestService, HttpRequestService>();
        services.AddSingleton<IScopeExecutor, ScopeExecutor>();

        // Guild player manager: one GuildPlayer (queue + consumer loop + voice client +
        // FFmpeg process) per guild, created lazily on the first enqueue, so multiple
        // servers can play music concurrently. Registered once and exposed both as the
        // hosted service and as the guild-scoped music facade used by commands.
        services.AddSingleton<GuildPlayerManager>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var config = sp.GetRequiredService<IConfiguration>();
            return new GuildPlayerManager(loggerFactory, _ =>
            {
                var ffmpeg = new FfmpegProcessService(loggerFactory.CreateLogger<FfmpegProcessService>(), config);
                var audioPlayer = new AudioPlayerService(ffmpeg, sp, loggerFactory.CreateLogger<AudioPlayerService>());
                return new GuildPlayerComponents(new MusicQueueService(), audioPlayer, ffmpeg);
            });
        });
        services.AddSingleton<IGuildMusicService>(sp => sp.GetRequiredService<GuildPlayerManager>());
        services.AddHostedService(sp => sp.GetRequiredService<GuildPlayerManager>());

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