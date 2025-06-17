using Domain;

namespace radio_discord_bot.Services.Interfaces;

public interface IBlacklistService
{
    Task AddToBlacklistAsync(string sourceUrl);
    Task RemoveFromBlacklistAsync(string title);
    Task<bool> IsBlacklistedAsync(string sourceUrl);
    Task<List<Song>> GetBlacklistedSongsAsync();
}