using Domain.Common;

namespace Domain.Entities;

public class PlayHistory(DateTimeOffset playedAt, ulong userId, Guid songId)
    : EntityBase
{
    public Guid Id { get; init; }

    public DateTimeOffset PlayedAt { get; init; } = playedAt;

    public ulong UserId { get; init; } = userId;
    public User? User { get; init; }

    public Guid SongId { get; init; } = songId;
    public Song? Song { get; init; }
    
    public static PlayHistory Create(DateTimeOffset playedAt, ulong userId, Guid songId)
    {
        return new PlayHistory(playedAt, userId, songId);
    }
}