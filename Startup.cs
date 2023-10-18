using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace radio_discord_bot;

public class Startup
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _serviceProvider;

    public Startup(DiscordSocketClient client, CommandService commands, IServiceProvider serviceProvider)
    {
        _client = client;
        _commands = commands;
        _serviceProvider = serviceProvider;
    }

    public async Task SetupLoggingAndReadyEvents()
    {
        await Task.CompletedTask;
        _client.Log += async (LogMessage log) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(log);
        };
        _client.Ready += async () =>
        {
            await Task.CompletedTask;
            Console.WriteLine($"Logged in as {_client.CurrentUser.Username}");
        };
    }

    public async Task SetupCommandHandling()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        _client.MessageReceived += async (arg) =>
        {
            _ = Task.Run(async () =>
            {
                if (arg is not SocketUserMessage msg) return;
                var context = new SocketCommandContext(_client, msg);
                int argPos = 0;

                var isHelpDM = context.IsPrivate && msg.ToString().Equals("help");

                if (msg.HasStringPrefix("/", ref argPos) || isHelpDM)
                {
                    var result = await _commands.ExecuteAsync(context, isHelpDM ? 0 : argPos, _serviceProvider);

                    if (!result.IsSuccess)
                    {
                        Console.WriteLine(result.ErrorReason);
                    }
                }
            });
        };
    }
}
