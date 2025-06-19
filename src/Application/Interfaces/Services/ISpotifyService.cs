namespace Application.Interfaces.Services;

public interface ISpotifyService
{
    Task GetRecommendationAsync(string songTitle);
}