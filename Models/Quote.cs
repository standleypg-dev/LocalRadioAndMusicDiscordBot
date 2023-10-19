using System.Text.Json.Serialization;

namespace radio_discord_bot.Models;

public class Quote
{
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;


    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;


    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

