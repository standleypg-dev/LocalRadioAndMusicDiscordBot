using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Application.DTOs.Spotify;

public class ArtistDto
{
    [JsonPropertyName("genres")]
    [field: AllowNull, MaybeNull]
    public string[] Genres
    {
        get => field ?? [];
        set => field = value.Take(5).ToArray();
    }
}