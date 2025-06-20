namespace Application.DTOs.Stats;

public class UserStatsDto
{
    public string Username { get; init; } = string.Empty;
    public int TotalPlays { get; init; }
    public int UniqueSongs { get; init; }
    public required DateTimeOffset MemberSince { get; init; }
}