using Domain.Common;
using Domain.Eventing;
using Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Eventing;

public sealed class AsyncEventDispatcher(IServiceProvider serviceProvider, HandlerRegistry handlerRegistry) : IAsyncEventDispatcher
{
    public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent
    {
        var tasks = handlerRegistry.GetAsyncHandlers(typeof(TEvent))
            .Select(t => ((IAsyncEventHandler<TEvent>)serviceProvider.GetRequiredService(t)).HandleAsync(@event, ct));
        
        await Task.WhenAll(tasks);
    }
}