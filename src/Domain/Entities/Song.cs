using System.Text.Json.Serialization;
using Domain.Common;

namespace Domain.Entities;

public class Song: EntityBase
{
    public Guid Id { get; private set; }
    public string SourceUrl { get; private set; }
    public string Title { get; private set; }
    public bool IsBlacklisted { get; private set; } = false;
    
    [JsonIgnore]
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