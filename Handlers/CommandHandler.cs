
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using radio_discord_bot.Helpers;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace radio_discord_bot.Handlers;

public class CommandHandler
{
    private readonly AudioService _audioService;
    private readonly YoutubeClient _youtubeClient;

    public CommandHandler(AudioService audioService, YoutubeClient youtubeClient)
    {
        _audioService = audioService;
        _youtubeClient = youtubeClient;
    }

    public async Task HandleCommand(string command, SocketTextChannel? channel = null, SocketUserMessage? message = null, IVoiceChannel? voiceChannel = null)
    {
        System.Console.WriteLine($"command: {command}");
        switch (command)
        {
            case "help":
            case "h":
                await channel!.SendMessageAsync(Constants.GET_HELP_MESSAGE);
                break;
            case string s when s.StartsWith("tutup"):
                await _audioService.DestroyVoiceChannelAsync(voiceChannel!);
                PlaylistService.playlist.Clear();
                PlaylistService.previousPlaylistLength = 0;
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
                        await _audioService.InitiateVoiceChannelAsyncYt(voiceChannel, uri.ToString());
                    }
                    else
                    {
                        var videos = await _youtubeClient.Search.GetVideosAsync(commandQuery).CollectAsync(5); // Adjust the number of results we want
                        // System.Console.WriteLine($"videos: {JsonSerializer.Serialize(videos.Select(x => x.Url))}");
                        var components = MessageComponentGenerator.GenerateComponents(videos.ToList());

                        await message.ReplyAsync("Click the button:", components: components);
                    }
                }
                break;
            case string s when s.StartsWith("next"):
                {
                    await _audioService.NextSongAsync();
                    await _audioService.OnPlaylistChangedAsync();
                }
                break;
            case string s when s.StartsWith("playlist"):
                {
                    var playlist = PlaylistService.playlist;
                    if (playlist.Count == 0)
                    {
                        await channel!.SendMessageAsync("Playlist puang");
                        return;
                    }
                    await message!.ReplyAsync("Playlist:\n" + string.Join('\n', playlist.Select(song => song.Url)));
                }
                break;
        }
    }
}
