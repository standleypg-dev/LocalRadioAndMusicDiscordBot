using System.Text.Json.Serialization;

namespace Application.DTOs;

public class JokeDto
{
    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("setup")]
    public string Setup { get; set; } = string.Empty;

    [JsonPropertyName("delivery")]
    public string Delivery { get; set; } = string.Empty;
}



