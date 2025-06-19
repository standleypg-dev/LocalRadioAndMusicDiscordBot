namespace Infrastructure.Interfaces.Repositories;

public interface ISongRepository
{
    Task RemoveFromBlacklistAsync(string songId);
    Task AddToBlacklistAsync(string sourceUrl);
}