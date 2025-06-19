namespace ApplicationDto.DTOs.Stats;

public class TopSong
{
    public string Title { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public int PlayCount { get; set; }
    public DateTime LastPlayed { get; set; }
}