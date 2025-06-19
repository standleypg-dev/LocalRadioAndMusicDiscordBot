using ApplicationDto.DTOs;
using ApplicationDto.DTOs.Stats;

namespace Application.Interfaces.Services;

public interface IStatisticsService<in TSocketUser, TSongDtoVoiceChannel> 
    where TSocketUser : class
    where TSongDtoVoiceChannel : class
{
    Task LogSongPlayAsync(TSocketUser socketUser, TSongDtoVoiceChannel playedSong);
    Task<List<TopSong>> GetUserTopSongsAsync(ulong userId, int limit = 10);
    Task<UserStats?> GetUserStatsAsync(ulong userId);
    Task<List<RecentPlay>> GetUserRecentPlaysAsync(ulong userId, int limit = 10);
    Task<List<TopSong>> GetTopSongsAsync(bool isToday = false, int limit = 10);
}