namespace radio_discord_bot.Models;

public abstract class HelpMessage
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
