using radio_discord_bot.Models.Spotify;

namespace radio_discord_bot.Services.Interfaces;

public interface ISpotifyService
{
    Task GetRecommendationAsync(string songTitle);
}