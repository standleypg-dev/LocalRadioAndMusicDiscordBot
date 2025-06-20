using System.Text.Json.Serialization;
using Domain.Common;

namespace Domain.Entities;

public class Song: EntityBase
{
    public Guid Id { get; init; }
    public string SourceUrl { get; init; }
    public string Title { get; init; }
    public bool IsBlacklisted { get; private set; } = false;
    
    public ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();

    private Song(string sourceUrl, string title)
    {
        SourceUrl = sourceUrl;
        Title = title;
    }
    
    public static Song Create(string sourceUrl, string title)
    {
        return new Song(sourceUrl, title);
    }

    public static Song MarkAsBlacklisted(Song song, bool isBlacklisted)
    {
        ArgumentNullException.ThrowIfNull(song, nameof(song));

        song.IsBlacklisted = isBlacklisted;
        return song;
    }
}