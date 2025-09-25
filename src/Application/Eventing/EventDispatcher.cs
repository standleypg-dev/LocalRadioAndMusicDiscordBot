using Domain.Common;
using Domain.Eventing;
using Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Eventing;

public class EventDispatcher(IServiceProvider serviceProvider, HandlerRegistry handlerRegistry) : IEventDispatcher
{
    public void Dispatch<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var tasks = handlerRegistry.GetSyncHandlers(@event.GetType());
        foreach (var handlerType in tasks)
        {
            var handler = (IEventHandler<TEvent>)serviceProvider.GetRequiredService(handlerType);
            handler.Handle(@event);
        }
    }
}