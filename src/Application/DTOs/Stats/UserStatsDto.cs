namespace Application.DTOs.Stats;

public class UserStatsDto
{
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int TotalPlays { get; init; }
    public int UniqueSongs { get; init; }
    public required DateTimeOffset MemberSince { get; init; }
    public DateTimeOffset? LastPlayed { get; set; } = null;
}