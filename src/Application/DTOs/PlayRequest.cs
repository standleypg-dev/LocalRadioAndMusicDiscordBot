namespace Application.DTOs;

public record PlayRequest<TContext>(TContext Context, Func<string, Task> Callbacks);