using Domain.Events;

namespace Domain.Eventing;

public interface IAsyncEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}