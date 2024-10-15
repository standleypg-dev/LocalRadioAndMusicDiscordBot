using Discord;
using radio_discord_bot.Models;

namespace radio_discord_bot.Services.Interfaces;

[Obsolete("This class is not used anymore. Use AudioPlayerService instead.")]
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
