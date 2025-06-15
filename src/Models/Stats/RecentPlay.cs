namespace radio_discord_bot.Models.Stats;

public class RecentPlay
{
    public string Title { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public DateTime PlayedAt { get; set; }
}