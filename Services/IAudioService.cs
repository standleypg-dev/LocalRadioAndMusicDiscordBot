using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using radio_discord_bot.Models;

namespace radio_discord_bot.Services;

public interface IAudioService
{
    Task InitiateVoiceChannelAsync(IVoiceChannel voiceChannel, string audioUrl, bool isYt = false);

    Task DestroyVoiceChannelAsync();
    Task NextSongAsync();
    void AddSong(Song song);
    List<Song> GetSongs();
    void RemoveFirstSong();
    Task OnPlaylistChanged();
}
