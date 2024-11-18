using System.Text.Json.Serialization;

namespace radio_discord_bot.Models.Spotify;

public class Search
{
    [JsonPropertyName("tracks")] 
    public Tracks Tracks { get; set; }
}