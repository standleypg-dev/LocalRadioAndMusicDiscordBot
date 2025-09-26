namespace Application.Interfaces.Services;

public interface IStreamService
{
    Task<string> GetAudioStreamUrlAsync(string url, CancellationToken cancellationToken);
    Task<string> GetVideoTitleAsync(string url, CancellationToken cancellationToken);
}