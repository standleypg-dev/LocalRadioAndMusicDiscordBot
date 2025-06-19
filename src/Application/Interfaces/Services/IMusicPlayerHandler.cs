namespace Application.Interfaces.Services;

public interface IMusicPlayerHandler
{
    Task PlaySongAsync(Guid songId);

    Task StopSongAsync();
    
    Task NextSongAsync();
}