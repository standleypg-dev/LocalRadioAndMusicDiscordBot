using Application.DTOs;
using Application.DTOs.Stats;

namespace Application.Interfaces.Services;

public interface IStatisticsService
{
    Task LogSongPlayAsync(ulong id, string userName, string globalName, SongDtoBase songDto);
    Task<List<TopSongDto>> GetUserTopSongsAsync(ulong userId, int limit = 10);
    Task<UserStatsDto?> GetUserStatsAsync(ulong userId);
    Task<List<RecentPlayDto>> GetUserRecentPlaysAsync(ulong userId, int limit = 10);
    Task<List<TopSongDto>> GetTopSongsAsync(bool isToday = false, int limit = 10);
    Task<List<TopSongDto>> GetAllSongsAsync();
}