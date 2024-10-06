using radio_discord_bot.Models;

namespace radio_discord_bot.Services.Interfaces;

public interface IQueueService
{
    Task AddSongAsync(Song song);
    Task SkipSongAsync();
    Task ClearQueueAsync();
    Task<List<string>> GetQueueAsync();

}