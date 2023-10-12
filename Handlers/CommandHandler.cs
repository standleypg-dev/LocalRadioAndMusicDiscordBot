
using System.ComponentModel;
using Discord;
using Discord.WebSocket;
using radio_discord_bot.Helpers;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace radio_discord_bot.Handlers;

public static class CommandHandler
{
    public static async Task HandleCommand(string command, YoutubeClient youtubeClient, AudioService audioService, SocketTextChannel? channel = null, SocketUserMessage? message = null, IVoiceChannel? voiceChannel = null)
    {
        System.Console.WriteLine($"command: {command}");
        switch (command)
        {
            case "help":
            case "h":
                await channel!.SendMessageAsync(Constants.GET_HELP_MESSAGE);
                break;
            case string s when s.StartsWith("tutup"):
                await audioService.DestroyVoiceChannelAsync(voiceChannel!);
                break;
            case string s when s.StartsWith("pasang"):
                {
                    string commandQuery = command.Substring(7);
                    if (commandQuery.Equals("radio"))
                    {
                        var components = MessageComponentGenerator.GenerateComponents(Constants.radios);

                        await message.ReplyAsync("Click the button:", components: components);
                        return;
                    }
                    else if (Uri.TryCreate(commandQuery, UriKind.Absolute, out var uri))
                    {
                        await audioService.InitiateVoiceChannelAsyncYt(voiceChannel, uri.ToString());
                    }
                    else
                    {
                        var videos = await youtubeClient.Search.GetVideosAsync(commandQuery).CollectAsync(5); // Adjust the number of results we want

                        var components = MessageComponentGenerator.GenerateComponents(videos.ToList());

                        await message.ReplyAsync("Click the button:", components: components);
                    }
                }
                break;
        }
    }
}
