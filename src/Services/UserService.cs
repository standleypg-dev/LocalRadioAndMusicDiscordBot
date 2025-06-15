using Data;
using Domain;
using Microsoft.EntityFrameworkCore;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services;

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
}