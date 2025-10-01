using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

public class ScopeExecutor(IServiceScopeFactory scopeFactory) : IScopeExecutor
{
    public async Task ExecuteAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = scopeFactory.CreateScope();
        await action(scope.ServiceProvider);
    }
    public void Execute(Action<IServiceProvider> action)
    {
        using var scope = scopeFactory.CreateScope();
        action(scope.ServiceProvider);
    }
}