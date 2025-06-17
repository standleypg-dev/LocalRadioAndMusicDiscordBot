using Data;
using Domain;
using Microsoft.EntityFrameworkCore;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services;

public class BlacklistService(DiscordBotContext context): IBlacklistService
{
    public Task AddToBlacklistAsync(string sourceUrl)
    {
        if (string.IsNullOrEmpty(sourceUrl))
        {
            throw new ArgumentException("Source URL cannot be null or empty.", nameof(sourceUrl));
        }

        var song = context.Songs.First(b => b.SourceUrl == sourceUrl);
        
        song = Song.MarkAsBlacklisted(song, true);
        context.Songs.Update(song);

        return context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Removes a song from the blacklist base on title.
    /// Use contains to match the title.
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    public Task RemoveFromBlacklistAsync(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException("Title cannot be null or empty.", nameof(title));
        }

        var song = context.Songs.First(b => EF.Functions.Like(b.Title.ToLower(), $"%{title.ToLower()}%") );
        
        song = Song.MarkAsBlacklisted(song, false);
        context.Songs.Update(song);

        return context.SaveChangesAsync();
    }

    /// <summary>
    /// Checks if a song is blacklisted based on its source URL.
    /// </summary>
    /// <param name="sourceUrl"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Task<bool> IsBlacklistedAsync(string sourceUrl)
    {
        if (string.IsNullOrEmpty(sourceUrl))
        {
            throw new ArgumentException("Source URL cannot be null or empty.", nameof(sourceUrl));
        }

        return context.Songs.AnyAsync(b => b.SourceUrl == sourceUrl && b.IsBlacklisted);
    }
 
    // Get all blacklisted songs
    public Task<List<Song>> GetBlacklistedSongsAsync()
    {
        return context.Songs.Where(b => b.IsBlacklisted).ToListAsync();
    }
}