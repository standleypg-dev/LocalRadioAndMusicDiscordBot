using Domain.Entities;

namespace Application.Interfaces.Services;

public interface IUserService
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByDisplayNameAsync(string displayName);
}