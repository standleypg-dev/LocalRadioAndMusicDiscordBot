namespace Application.DTOs;

// Non-generic base type (holds members that don't depend on T)
public abstract class PlayRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int RetryCount { get; set; }
    public Func<string, Task> Callbacks { get; set; } = null!;

    public abstract object ContextAsObject { get; }
}

public class PlayRequest<TContext> : PlayRequest
{
    public TContext Context { get; init; } = default!;

    public override object ContextAsObject => Context!;
}
