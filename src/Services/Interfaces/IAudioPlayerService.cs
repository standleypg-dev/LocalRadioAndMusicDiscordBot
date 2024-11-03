using Discord;

namespace radio_discord_bot.Services.Interfaces;

public interface IAudioPlayerService
{
    Task InitiateVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl, bool isYt = false);
    Task NextSongAsync();
    Task DestroyVoiceChannelAsync();
    Task OnPlaylistChanged();
    IVoiceChannel? GetBotCurrentVoiceChannel();
}