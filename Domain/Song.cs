using System.Text.Json.Serialization;
using Domain.Base;

namespace Domain;

public class Song: EntityBase
{
    public Guid Id { get; private set; }
    public string SourceUrl { get; private set; }
    public string Title { get; private set; }
    
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
}