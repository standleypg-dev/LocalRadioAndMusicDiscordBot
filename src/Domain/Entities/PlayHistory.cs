using Domain.Common;

namespace Domain.Entities;

public class PlayHistory(DateTimeOffset playedAt, ulong userId, Guid songId)
    : EntityBase
{
    public Guid Id { get; init; }

    public DateTimeOffset PlayedAt { get; set; } = playedAt;

    public ulong UserId { get; init; } = userId;
    public User User { get; init; } = null!;

    public Guid SongId { get; init; } = songId;
    public Song Song { get; init; } = null!;
    
    public int TotalPlays { get; private set; } = 1;
    
    public static PlayHistory Create(DateTimeOffset playedAt, ulong userId, Guid songId)
    {
        return new PlayHistory(playedAt, userId, songId);
    }
    
    public static PlayHistory UpdateTotalPlays(PlayHistory playHistory)
    {
        ArgumentNullException.ThrowIfNull(playHistory);

        playHistory.PlayedAt = DateTimeOffset.UtcNow;
        playHistory.TotalPlays += 1;
        return playHistory;
    }
}