using System.Text.Json.Serialization;

namespace Application.DTOs.Spotify;

public class RecommendationDto
{
    [JsonPropertyName("tracks")] 
    public Items[] Tracks { get; set; } = [];
}
