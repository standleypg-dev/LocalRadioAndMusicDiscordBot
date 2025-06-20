namespace Application.DTOs.Stats;

public class RecentPlayDto
{
    public string Title { get; init; } = string.Empty;
    public required DateTimeOffset PlayedAt { get; init; }
}