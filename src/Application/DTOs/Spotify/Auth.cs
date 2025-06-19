using System.Text.Json.Serialization;

namespace ApplicationDto.DTOs.Spotify;

public class Auth
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.Now;
}