using Domain.Common;

namespace Domain.Entities;

public class RadioSource: EntityBase
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string SourceUrl { get; private set; }
    public bool IsActive { get; set; } = true;
    
    private RadioSource(string name, string sourceUrl)
    {
        Name = name;
        SourceUrl = sourceUrl;
    }
    
    public static RadioSource UpdateSourceUrl(RadioSource radioSource, string newSourceUrl, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(radioSource, nameof(radioSource));
        ArgumentNullException.ThrowIfNull(newSourceUrl, nameof(newSourceUrl));

        radioSource.SourceUrl = newSourceUrl;
        radioSource.IsActive = isActive;
        return radioSource;
    }
    
    public static RadioSource Create(string name, string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }
        
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            throw new ArgumentException("Source URL cannot be null or empty.", nameof(sourceUrl));
        }

        return new RadioSource(name, sourceUrl);
    }
    
    public static RadioSource UpdateIsActive(RadioSource radioSource, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(radioSource, nameof(radioSource));

        radioSource.IsActive = isActive;
        return radioSource;
    }
}