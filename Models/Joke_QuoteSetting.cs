namespace radio_discord_bot.Models;

public abstract class JokeQuoteSetting
{
    public string Greeting { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
}
