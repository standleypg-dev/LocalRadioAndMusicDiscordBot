using Domain.Base;

namespace Domain;

public class PlayHistory: EntityBase
{
    public Guid Id { get; private set; }

    public DateTime PlayedAt
    {
        get
        {
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore");
            return TimeZoneInfo.ConvertTimeFromUtc(field, malaysiaTimeZone);
        }
        
        private set;
    }
    
    public ulong UserId { get; private set; }
    public User User { get; private set; }
    
    public Guid SongId { get; private set; }
    public Song Song { get; private set; }
    
    private PlayHistory()
    {
        // EF Core requires a parameterless constructor for entity instantiation
    }
    private PlayHistory(DateTime playedAt, User user, Song song)
    {
        PlayedAt = playedAt;
        User = user;
        Song = song;
    }
    
    public static PlayHistory Create(DateTime playedAt, User user, Song song)
    {
        return new PlayHistory(playedAt, user, song);
    }
}