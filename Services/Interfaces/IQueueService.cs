using radio_discord_bot.Models;

namespace radio_discord_bot.Services.Interfaces;

public interface IQueueService
{
    event OnSongAdded? SongAdded;
    Task AddSongAsync(Song song);
    Task SkipSongAsync();
    Task ClearQueueAsync();
    Task<List<Song>> GetQueueAsync();

}