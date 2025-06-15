namespace radio_discord_bot.Models.Stats;

public class UserStats
{
    public string Username { get; set; } = string.Empty;
    public int TotalPlays { get; set; }
    public int UniqueSongs { get; set; }
    public DateTime MemberSince { get; set; }
}