using Domain.Entities;

namespace Application.Interfaces.Services;

public interface IBlacklistService
{
    Task AddToBlacklistAsync(string sourceUrl);
    Task RemoveFromBlacklistAsync(string title);
    Task<bool> IsBlacklistedAsync(string sourceUrl);
    Task<List<Song>> GetBlacklistedSongsAsync();
}