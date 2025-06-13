using Discord;
using Discord.WebSocket;

namespace radio_discord_bot.Models;

public class Song
{
    public string Url { get; set; } = string.Empty;
    public SocketVoiceChannel? VoiceChannel { get; init; }
    public string Title { get; set; } = string.Empty;
}