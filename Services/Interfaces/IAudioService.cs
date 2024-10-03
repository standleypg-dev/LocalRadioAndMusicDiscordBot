using Discord;
using radio_discord_bot.Models;

namespace radio_discord_bot.Services.Interfaces;

public interface IAudioService
{
    Task InitiateVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl, bool isYt = false);

    Task DestroyVoiceChannelAsync();
    Task NextSongAsync();
    Task AddSong(Song song);
    List<Song> GetSongs();
    void RemoveFirstSong();
    Task OnPlaylistChanged();
    Task EmptyPlaylist();
    IVoiceChannel? GetBotCurrentVoiceChannel();
    Task<string> GetYoutubeTitle(string url);
}
