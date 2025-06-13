using Discord.WebSocket;

namespace radio_discord_bot.Services.Interfaces;

public interface IAudioPlayerService
{
    Task InitiateVoiceChannelAsync(SocketVoiceChannel? voiceChannel, string audioUrl, bool isYt = false);
    Task NextSongAsync();
    Task DestroyVoiceChannelAsync();
    Task OnPlaylistChanged();
    SocketVoiceChannel? GetBotCurrentVoiceChannel();
}