namespace Application.Interfaces.Services;

public interface IScopeExecutor
{
    Task ExecuteAsync(Func<IServiceProvider, Task> action);
    void Execute(Action<IServiceProvider> action);
}