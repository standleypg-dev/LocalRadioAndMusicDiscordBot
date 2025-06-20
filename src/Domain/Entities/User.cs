using System.Text.Json.Serialization;
using Domain.Common;

namespace Domain.Entities;

public class User: EntityBase
{
    public ulong Id { get; init; }
    public string Username { get; init; }
    public string DisplayName { get; init; }
    public int TotalSongsPlayed { get; set; }
    
    public ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();
    
    private User(ulong id, string username, string displayName)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }
        
        Id = id;
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

        user.TotalSongsPlayed += 1;
        return user;
    }
}