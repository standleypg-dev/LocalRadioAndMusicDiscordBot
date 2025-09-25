using Domain.Events;

namespace Domain.Eventing;

public interface IAsyncEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IEvent;
}