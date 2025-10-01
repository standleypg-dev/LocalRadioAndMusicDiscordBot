namespace Application.Interfaces.Services;

public interface IRandomService
{
    Task<string> GetAsync();
}
