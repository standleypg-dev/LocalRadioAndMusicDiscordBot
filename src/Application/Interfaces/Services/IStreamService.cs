namespace Application.Interfaces.Services;

public interface IStreamService
{
    Task<string> GetAudioStreamUrlAsync(string url);
    Task<string> GetVideoTitleAsync(string url);
}