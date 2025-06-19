namespace Application.Interfaces.Services;

public interface IYoutubeService
{
    Task<string> GetAudioStreamUrlAsync(string url);
    Task<string> GetVideoTitleAsync(string url);
}