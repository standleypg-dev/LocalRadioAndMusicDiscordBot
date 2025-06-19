using System.Text.Json.Serialization;

namespace ApplicationDto.DTOs.Spotify;

public class Recommendation
{
    [JsonPropertyName("tracks")] 
    public Items[] Tracks { get; set; }
}
