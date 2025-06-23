using System.Text.Json.Serialization;

namespace Application.DTOs.Spotify;

public class SearchDto
{
    [JsonPropertyName("tracks")] 
    public TracksDto Tracks { get; set; } = new();
}