namespace radio_discord_bot.Services.Interfaces;

public interface IJokeService
{
    Task<string> GetJokeAsync();
}
