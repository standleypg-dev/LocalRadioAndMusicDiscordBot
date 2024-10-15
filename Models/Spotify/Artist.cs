using System.Text.Json.Serialization;

namespace radio_discord_bot.Models.Spotify;

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