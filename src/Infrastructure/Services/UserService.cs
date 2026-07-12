using Application.DTOs.Stats;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class UserService(DiscordBotContext context) : IUserService
{
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        return user;
    }

    public async Task<User?> GetUserByDisplayNameAsync(string displayName)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.DisplayName == displayName);

        return user;
    }
    
    public async Task<ICollection<UserStatsDto>> GetAllUsersAsync()
    {
        var users = await context.Users
            .Select(u => new UserStatsDto
            {
                Username = u.Username,
                MemberSince = u.CreatedAt,
                TotalPlays = u.PlayHistories.Sum(ph => ph.TotalPlays),
                UniqueSongs = u.PlayHistories.Select(ph => ph.Song).Distinct().Count(),
                LastPlayed = u.PlayHistories
                    .OrderByDescending(ph => ph.PlayedAt)
                    .Select(ph => (DateTimeOffset?)ph.PlayedAt)
                    .FirstOrDefault(),
                DisplayName = u.DisplayName ?? u.Username,
                RecentSongs = u.PlayHistories
                    .OrderByDescending(ph => ph.PlayedAt)
                    .Take(10)
                    .Select(ph => new RecentSongDto
                    {
                        Title = ph.Song.Title,
                        TotalPlays = ph.TotalPlays,
                        PlayedAt = ph.PlayedAt
                    })
                    .ToList()
            })
            .OrderByDescending(u => u.TotalPlays)
            .ToListAsync();

        return users;
    }
}