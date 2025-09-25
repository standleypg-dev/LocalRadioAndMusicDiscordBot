using Domain.Events;

namespace Domain.Eventing;

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    void Handle(TEvent @event);
}