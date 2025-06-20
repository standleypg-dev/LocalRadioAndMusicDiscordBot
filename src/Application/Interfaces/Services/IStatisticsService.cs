using Application.DTOs.Stats;

namespace Application.Interfaces.Services;

public interface IStatisticsService<in TSocketUser, in TSongDtoVoiceChannel> 
    where TSocketUser : class
    where TSongDtoVoiceChannel : class
{
    Task LogSongPlayAsync(TSocketUser socketUser, TSongDtoVoiceChannel playedSong);
    Task<List<TopSongDto>> GetUserTopSongsAsync(ulong userId, int limit = 10);
    Task<UserStatsDto?> GetUserStatsAsync(ulong userId);
    Task<List<RecentPlayDto>> GetUserRecentPlaysAsync(ulong userId, int limit = 10);
    Task<List<TopSongDto>> GetTopSongsAsync(bool isToday = false, int limit = 10);
}