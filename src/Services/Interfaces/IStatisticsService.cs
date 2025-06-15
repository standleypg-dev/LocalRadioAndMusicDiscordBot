using Discord.WebSocket;
using radio_discord_bot.Models;
using radio_discord_bot.Models.Stats;

namespace radio_discord_bot.Services.Interfaces;

public interface IStatisticsService
{
    Task LogSongPlayAsync(SocketUser socketUser, Song playedSong);
    Task<List<TopSong>> GetUserTopSongsAsync(ulong userId, int limit = 10);
    Task<UserStats?> GetUserStatsAsync(ulong userId);
    Task<List<RecentPlay>> GetUserRecentPlaysAsync(ulong userId, int limit = 10);
    Task<List<TopSong>> GetTopSongsAsync(bool isToday = false, int limit = 10);
}