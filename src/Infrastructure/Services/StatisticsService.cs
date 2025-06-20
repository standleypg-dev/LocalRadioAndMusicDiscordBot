using Application.DTOs;
using Application.DTOs.Stats;
using Application.Interfaces.Services;
using Discord.WebSocket;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Song = Domain.Entities.Song;

namespace Infrastructure.Services;

public class StatisticsService(DiscordBotContext context, IYoutubeService youtubeService): IStatisticsService<SocketUser, SongDto<SocketVoiceChannel>>
{
    // Log when a user plays a song
    public async Task LogSongPlayAsync(SocketUser socketUser, SongDto<SocketVoiceChannel> playedSong)
    {
        var userId = socketUser.Id;
        var username = socketUser.Username;
        var displayName = socketUser.GlobalName;
        var sourceUrl = playedSong.Url;
        
        try
        {
            // Ensure user exists
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                user = User.Create(userId, username, displayName);
                context.Users.Add(user);
            }
            
            // Find or create song
            
            var song = await context.Songs.FirstOrDefaultAsync(s => s.SourceUrl == sourceUrl);
                
            if (song == null)
            {
                var songTitle = await youtubeService.GetVideoTitleAsync(sourceUrl);
                song = Song.Create(sourceUrl, songTitle);
                context.Songs.Add(song);
            }
            
            // Save to get song ID if it's new
            await context.SaveChangesAsync();
            
            // Log the play
            var playHistory = PlayHistory.Create(DateTimeOffset.UtcNow, user.Id, song.Id);
            
            context.PlayHistory.Add(playHistory);
            
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
            .GroupBy(ph => new { ph.SongId, ph.Song.Title})
            .Select(g => new TopSongDto
            {
                Title = g.Key.Title,
                PlayCount = g.Count(),
                LastPlayed = g.Max(ph => ph.PlayedAt)
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
        
        var totalSongs = await context.PlayHistory.CountAsync(ph => ph.UserId == userId);
        
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
            var today = DateTimeOffset.UtcNow;
            query = query.Where(ph => ph.PlayedAt >= today);
        }
        
        return await query
            .GroupBy(ph => new { ph.SongId, ph.Song.Title })
            .Select(g => new TopSongDto
            {
                Title = g.Key.Title,
                PlayCount = g.Count(),
                LastPlayed = g.Max(ph => ph.PlayedAt)
            })
            .OrderByDescending(ts => ts.PlayCount)
            .Take(limit)
            .ToListAsync();
    }
    
    
}