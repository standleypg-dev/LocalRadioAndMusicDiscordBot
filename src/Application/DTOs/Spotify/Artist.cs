using System.Text.Json.Serialization;

namespace ApplicationDto.DTOs.Spotify;

public class Artist
{
    private string[]? _genres;

    [JsonPropertyName("genres")]
    public string[] Genres
    {
        get => _genres ?? [];
        set => _genres = value.Take(5).ToArray();
    }
}