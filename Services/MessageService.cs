using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using radio_discord_bot.Handlers;
using YoutubeExplode;

namespace radio_discord_bot.Services;

public class MessageService
{
    private readonly CommandHandler _commandHandler;
    private readonly YoutubeClient _youtubeClient;
    private readonly AudioService _audioService;

    public MessageService(CommandHandler commandHandler, YoutubeClient youtubeClient, AudioService audioService)
    {
        _commandHandler = commandHandler;
        _youtubeClient = youtubeClient;
        _audioService = audioService;
    }

    public async Task TextChannelMessageReceivedAsync(SocketMessage arg)
    {
        await Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            var message = arg as SocketUserMessage;
            if (message.Author.IsBot) return;
            var channel = message.Channel as SocketTextChannel;
            System.Console.WriteLine(message.Author as IGuildUser);
            if (message.ToString().StartsWith("/"))
            {
                var voiceChannel = (message.Author as IGuildUser)?.VoiceChannel;

                if (voiceChannel == null)
                {
                    await channel.SendMessageAsync("Nuan mesti ba dalam voice channel enti ka masang ngena command tu.");
                    return;
                }

                var command = message.ToString().Substring(1).Trim();
                System.Console.WriteLine($"command: {command}");

                await _commandHandler.HandleCommand(command, _youtubeClient, _audioService, channel, message, voiceChannel);
            }
        });
    }
}
