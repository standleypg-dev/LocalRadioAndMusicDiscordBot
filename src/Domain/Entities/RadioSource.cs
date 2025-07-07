using Domain.Common;

namespace Domain.Entities;

public class RadioSource: EntityBase
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string SourceUrl { get; private set; }
    
    private RadioSource(string name, string sourceUrl)
    {
        Name = name;
        SourceUrl = sourceUrl;
    }
    
    public static RadioSource UpdateSourceUrl(RadioSource radioSource, string newSourceUrl)
    {
        ArgumentNullException.ThrowIfNull(radioSource, nameof(radioSource));
        ArgumentNullException.ThrowIfNull(newSourceUrl, nameof(newSourceUrl));

        radioSource.SourceUrl = newSourceUrl;
        return radioSource;
    }
}