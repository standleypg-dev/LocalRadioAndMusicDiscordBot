using Application.DTOs;
using Application.DTOs.Stats;
using Application.Interfaces.Services;
using Discord.WebSocket;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Song = Domain.Entities.Song;

namespace Infrastructure.Services;

public class StatisticsService(DiscordBotContext context, IYoutubeService youtubeService)
    : IStatisticsService<SocketUser, SongDto<SocketVoiceChannel>>
{
    // Log when a user plays a song


    public async Task LogSongPlayAsync(ulong id, string userName, string globalName, SongDto<SocketVoiceChannel> songDto)
    {
        try
        {
            // Ensure user exists
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                user = User.Create(id, userName, globalName);
                context.Users.Add(user);
            }

            // Find or create song

            var song = await context.Songs
                           .FirstOrDefaultAsync(s => EF.Functions.Like(s.Title, songDto.Title)) ??
                       await context.Songs
                           .FirstOrDefaultAsync(s => s.SourceUrl == songDto.Url);

            if (song == null)
            {
                var songTitle = await youtubeService.GetVideoTitleAsync(songDto.Url);
                song = Song.Create(songDto.Url, songTitle);
                context.Songs.Add(song);
            }

            // Save to get song ID if it's new
            await context.SaveChangesAsync();
            
            var existingPlayHistory = await context.PlayHistory
                .FirstOrDefaultAsync(ph => ph.UserId == user.Id && ph.SongId == song.Id);
            
            if (existingPlayHistory != null)
            {
                PlayHistory.UpdateTotalPlays(existingPlayHistory);
                context.PlayHistory.Update(existingPlayHistory);
            }
            else
            {
                // Create new play history entry
                var playHistory = PlayHistory.Create(DateTimeOffset.UtcNow, user.Id, song.Id);
                context.PlayHistory.Add(playHistory);
            }
            
            // Update user's total play count
            user = User.UpdateTotalSongsPlayed(user);
            context.Users.Update(user);

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't break the music bot
            Console.WriteLine($"Error logging song play: {ex.Message}");
        }
    }

    public async Task<List<TopSongDto>> GetUserTopSongsAsync(ulong userId, int limit = 10)
    {
        return await context.PlayHistory
            .Where(ph => ph.UserId == userId)
            .Select(ph => new TopSongDto
            {
                Title = ph.Song.Title,
                PlayCount = ph.TotalPlays,
                LastPlayed = ph.PlayedAt
            })
            .OrderByDescending(uts => uts.PlayCount)
            .Take(limit)
            .ToListAsync();
    }

    // Get user's total stats
    public async Task<UserStatsDto?> GetUserStatsAsync(ulong userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        // count based on total plays props
        var totalSongs = await context.PlayHistory
            .Where(ph => ph.UserId == userId)
            .SumAsync(ph => ph.TotalPlays);

        var uniqueSongs = await context.PlayHistory
            .Where(ph => ph.UserId == userId)
            .Select(ph => ph.SongId)
            .Distinct()
            .CountAsync();

        return new UserStatsDto
        {
            Username = user.Username,
            TotalPlays = totalSongs,
            UniqueSongs = uniqueSongs,
            MemberSince = user.CreatedAt
        };
    }

    // Get user's recent activity
    public async Task<List<RecentPlayDto>> GetUserRecentPlaysAsync(ulong userId, int limit = 10)
    {
        return await context.PlayHistory
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.PlayedAt)
            .Select(ph => new RecentPlayDto
            {
                Title = ph.Song.Title,
                PlayedAt = ph.PlayedAt
            })
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<TopSongDto>> GetTopSongsAsync(bool isToday = false, int limit = 10)
    {
        var query = context.PlayHistory.AsQueryable();

        if (isToday)
        {
            var localToday = DateTime.Today;
            var offset = TimeZoneInfo.Local.GetUtcOffset(localToday);
            var localDateTime = new DateTimeOffset(localToday, offset);
            var utcTodayStart = localDateTime.ToUniversalTime(); 
            query = query.Where(ph => ph.PlayedAt >= utcTodayStart);
        }

        return await query
            .GroupBy(ph => new { ph.SongId, ph.Song.Title })
            .Select(g => new TopSongDto
            {
                Title = g.Key.Title,
                PlayCount = g.Sum(ph => ph.TotalPlays),
                LastPlayed = g.Max(ph => ph.PlayedAt)
            })
            .OrderByDescending(ts => ts.PlayCount)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<TopSongDto>> GetAllSongsAsync()
    {
        return await context.PlayHistory
            .GroupBy(ph => new { ph.SongId, ph.Song.Title })
            .Select(g => new TopSongDto
            {
                Title = g.Key.Title,
                PlayCount = g.Sum(ph => ph.TotalPlays),
                LastPlayed = g.Max(ph => ph.PlayedAt)
            })
            .OrderByDescending(ts => ts.PlayCount)
            .ToListAsync();
    }
}