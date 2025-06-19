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
}