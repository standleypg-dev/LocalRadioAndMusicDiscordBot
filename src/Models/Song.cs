using Discord;

namespace radio_discord_bot.Models;

public class Song
{
    public string Url { get; set; } = string.Empty;
    public IVoiceChannel? VoiceChannel { get; init; }
    public string Title { get; set; } = string.Empty;
}