using Domain.Common;

namespace Domain.Entities;

public class RadioSource: EntityBase
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public string SourceUrl { get; private set; }
    public bool IsActive { get; set; } = true;
    
    private RadioSource(string name, string sourceUrl)
    {
        Name = name;
        SourceUrl = sourceUrl;
    }
    
    public static void Update(RadioSource radioSource, string name, string newSourceUrl, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(radioSource);
        ArgumentNullException.ThrowIfNull(radioSource);
        ArgumentNullException.ThrowIfNull(newSourceUrl);

        radioSource.Name = name;
        radioSource.SourceUrl = newSourceUrl;
        radioSource.IsActive = isActive;
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