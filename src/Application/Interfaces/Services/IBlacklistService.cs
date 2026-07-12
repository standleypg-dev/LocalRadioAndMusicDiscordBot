using Domain.Entities;

namespace Application.Interfaces.Services;

public interface IBlacklistService
{
    /// <summary>
    /// Marks the song with the given source URL as blacklisted.
    /// Returns false when no song with that URL exists.
    /// </summary>
    Task<bool> AddToBlacklistAsync(string sourceUrl);

    /// <summary>
    /// Removes the first song whose title contains the given text from the blacklist.
    /// Returns false when no matching song exists.
    /// </summary>
    Task<bool> RemoveFromBlacklistAsync(string title);

    Task<bool> IsBlacklistedAsync(string sourceUrl);
    Task<List<Song>> GetBlacklistedSongsAsync();
}
