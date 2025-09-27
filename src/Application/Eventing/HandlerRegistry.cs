using System.Collections.Concurrent;

namespace Application.Eventing;

public class HandlerRegistry
{
    private readonly ConcurrentDictionary<Type, (Type[] sync, Type[] async)> _map = new();

    public void Register(Type eventType, Type handlerType, bool isAsync = true)
    {
        _map.AddOrUpdate(eventType,
            _ => isAsync ? ([], [handlerType])
                : ([handlerType], []),
            (_, tuple) => isAsync
                ? (tuple.sync, tuple.async.Concat([handlerType]).ToArray())
                : (tuple.sync.Concat([handlerType]).ToArray(), tuple.async));
    }
    
    public IEnumerable<Type> GetSyncHandlers(Type eventType) => 
        _map.TryGetValue(eventType, out var handlers) ? handlers.sync : [];
    
    public IEnumerable<Type> GetAsyncHandlers(Type eventType) => 
        _map.TryGetValue(eventType, out var handlers) ? handlers.async : [];
}