using Domain;

namespace radio_discord_bot.Services.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByDisplayNameAsync(string displayName);
}