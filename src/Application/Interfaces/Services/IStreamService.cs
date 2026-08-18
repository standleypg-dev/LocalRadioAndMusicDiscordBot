namespace Application.Interfaces.Services;

public interface IStreamService
{
    Task<string> GetAudioStreamUrlAsync(string url, int attempt, CancellationToken cancellationToken);
    Task<string> GetVideoTitleAsync(string url, CancellationToken cancellationToken);
}