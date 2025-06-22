namespace Application.DTOs;

public class SongDto<TVoiceChannel> where TVoiceChannel : class
{
    public required string Url { get; set; } = string.Empty;
    public required TVoiceChannel? VoiceChannel { get; init; }
    public string Title { get; set; } = string.Empty;
    public required ulong UserId { get; init; } 
}