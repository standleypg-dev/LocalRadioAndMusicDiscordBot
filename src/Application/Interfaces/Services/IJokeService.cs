namespace Application.Interfaces.Services;

public interface IJokeService
{
    Task<string> GetJokeAsync();
}
