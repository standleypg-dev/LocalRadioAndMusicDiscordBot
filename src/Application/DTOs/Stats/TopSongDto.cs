namespace Application.DTOs.Stats;

public class TopSongDto
{
    public string Title { get; init; } = string.Empty;
    public string? Artist { get; set; }
    public int PlayCount { get; init; }
    public required DateTimeOffset LastPlayed { get; init; }
}