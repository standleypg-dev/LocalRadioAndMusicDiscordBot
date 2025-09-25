using Domain.Events;

namespace Domain.Eventing;

public interface IEventDispatcher
{
    void Dispatch<TEvent>(TEvent @event) where TEvent : IEvent;
}