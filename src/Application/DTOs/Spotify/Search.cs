using System.Text.Json.Serialization;

namespace ApplicationDto.DTOs.Spotify;

public class Search
{
    [JsonPropertyName("tracks")] 
    public Tracks Tracks { get; set; }
}