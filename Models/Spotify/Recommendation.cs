using System.Text.Json.Serialization;

namespace radio_discord_bot.Models.Spotify;

public class Recommendation
{
    [JsonPropertyName("tracks")] 
    public BaseSearch[] Tracks { get; set; }
}
