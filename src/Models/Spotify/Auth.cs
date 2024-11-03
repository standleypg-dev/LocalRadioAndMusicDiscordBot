using System.Text.Json.Serialization;

namespace radio_discord_bot.Models.Spotify;

public class Auth
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.Now;
}