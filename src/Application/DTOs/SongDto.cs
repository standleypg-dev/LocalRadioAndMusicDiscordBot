namespace Application.DTOs;

public class SongDto<TVoiceChannel> where TVoiceChannel : class
{
    public string Url { get; set; } = string.Empty;
    public TVoiceChannel? VoiceChannel { get; init; }
    public string Title { get; set; } = string.Empty;
}