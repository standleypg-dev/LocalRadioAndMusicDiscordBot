using Discord;
using Discord.Commands;
using Discord.WebSocket;
using radio_discord_bot.Commands;
using radio_discord_bot.Configs;

namespace radio_discord_bot;

public class Startup(DiscordSocketClient client, CommandService commands, IServiceProvider serviceProvider, ILogger<Startup> logger, IWebHostEnvironment environment, IConfiguration configuration)
{
    public async Task SetupLoggingAndReadyEvents()
    {
        await Task.CompletedTask;
        client.Log += LogAsync;
        commands.Log += LogAsync;
        client.Ready += async () =>
        {
            await Task.CompletedTask;
            logger.LogInformation($"Logged in as {client.CurrentUser.Username}");
        };
    }

    public async Task SetupCommandHandling()
    {
        if (environment.IsProduction())
        {
            await commands.AddModuleAsync<ProdRadioCommands>(serviceProvider);
            logger.LogInformation("Registered production commands");
        }
        else
        {
            await commands.AddModuleAsync<DevRadioCommands>(serviceProvider);
            logger.LogInformation("Registered development commands");
        }

        client.MessageReceived += MessageReceived;
    }

    private async Task MessageReceived(SocketMessage arg)
    {
        await Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            if (arg is not SocketUserMessage msg) return;
            var context = new SocketCommandContext(client, msg);
            var argPos = 0;

            var isHelpDm = context.IsPrivate && msg.ToString().Equals("help");

            var commandPrefix = ConfigurationHelper.GetConfiguration<string>(configuration, "Discord:Prefix");
            if (msg.HasStringPrefix(commandPrefix, ref argPos) || isHelpDm)
            {
                var result = await commands.ExecuteAsync(context, isHelpDm ? 0 : argPos, serviceProvider);

                if (!result.IsSuccess)
                {
                    logger.LogError($"OnMesssageReceived: {result.ErrorReason}");
                }
            }
        });
    }
    
    private Task LogAsync(LogMessage log)
    {
        var logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        logger.Log(logLevel, log.Exception, "[{Source}] {Message}", log.Source, log.Message);
        
        foreach (var module in commands.Modules)
        {
            logger.LogInformation("Module: {ModuleName}", module.Name);
            foreach (var cmd in module.Commands)
            {
                logger.LogInformation("Command: {CmdName} | Aliases: {Join}", cmd.Name, string.Join(", ", cmd.Aliases));
            }
        }
        return Task.CompletedTask;
    }
}