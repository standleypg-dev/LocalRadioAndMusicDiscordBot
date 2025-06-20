using System.Text.Json.Serialization;

namespace Application.DTOs.Spotify;

public class TracksDto
{
    [JsonPropertyName("items")] 
    public Items[] Items { get; set; } = [];
}

public class Items : BaseSearch
{
    [JsonPropertyName("artists")]
    public Artists[] Artists { get; set; } = [];
}

public class Artists : BaseSearch;

public class BaseSearch
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}