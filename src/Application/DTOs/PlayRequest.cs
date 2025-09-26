namespace Application.DTOs;

public record PlayRequest<TContext>(TContext Ctx, Func<Task> Callbacks);