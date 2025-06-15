using System.Text.Json.Serialization;
using Domain.Base;

namespace Domain;

public class User: EntityBase
{
    public ulong Id { get; private set; }
    public string Username { get; private set; }
    public string DisplayName { get; private set; }
    public int TotalSongsPlayed { get; private set; }
    
    [JsonIgnore]
    public ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();
    
    private User()
    {
        // EF Core requires a parameterless constructor for entity instantiation
    }
    
    private User(ulong userId, string username, string displayName)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }
        
        Id = userId;
        DisplayName = displayName; 
        Username = username;
        TotalSongsPlayed = 0;
    }
    
    public static User Create(ulong userId, string username, string displayName)
    {
        return new User(userId, username, displayName);
    }
    
    public static User UpdateTotalSongsPlayed(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Increment the total songs played count
        user.TotalSongsPlayed += 1;
        return user;
    }
}