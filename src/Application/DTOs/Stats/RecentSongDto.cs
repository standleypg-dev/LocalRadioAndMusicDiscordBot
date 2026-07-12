namespace Application.DTOs.Stats;

public class RecentSongDto
{
    public string Title { get; init; } = string.Empty;
    public int TotalPlays { get; init; }
    public required DateTimeOffset PlayedAt { get; init; }
}
