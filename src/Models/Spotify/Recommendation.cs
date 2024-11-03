using System.Text.Json.Serialization;

namespace radio_discord_bot.Models.Spotify;

public class Recommendation
{
    [JsonPropertyName("tracks")] 
    public Items[] Tracks { get; set; }
}
