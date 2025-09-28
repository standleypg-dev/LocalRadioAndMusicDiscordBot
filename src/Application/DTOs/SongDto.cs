namespace Application.DTOs;

public class SongDto<TVoiceChannel> : SongDtoBase where TVoiceChannel : class
{
    public required TVoiceChannel? VoiceChannel { get; init; }
}

public class SongDtoBase
{
    public required string Url { get; set; }
    public string? Title { get; set; }
    public required ulong UserId { get; init; } 
}