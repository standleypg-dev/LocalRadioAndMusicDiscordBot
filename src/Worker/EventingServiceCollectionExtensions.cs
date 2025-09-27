using System.Reflection;
using Application.Eventing;
using Domain.Eventing;

namespace Worker;

public static class EventingServiceCollectionExtensions
{
    public static void AddEventing(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Get all handler types once
        var handlerTypes = GetEventHandlerTypes(assemblies);

        // One singleton registry, populated via factory
        services.AddSingleton<HandlerRegistry>(_ => BuildRegistry(handlerTypes));

        // Dispatcher uses the same registry and current scope's services
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddScoped<IAsyncEventDispatcher, AsyncEventDispatcher>();

        // Register all handler types found by scanning
        foreach (var type in handlerTypes)
        {
            services.AddScoped(type);
        }
    }

    private static HandlerRegistry BuildRegistry(IEnumerable<Type> handlerTypes)
    {
        var registry = new HandlerRegistry();

        handlerTypes
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(IEventHandler<>) ||
                             i.GetGenericTypeDefinition() == typeof(IAsyncEventHandler<>)))
                .Select(i => new { Interface = i, HandlerType = type }))
            .ToList()
            .ForEach(item =>
            {
                var isAsync = item.Interface.GetGenericTypeDefinition() == typeof(IAsyncEventHandler<>);
                registry.Register(item.Interface.GetGenericArguments()[0], item.HandlerType, isAsync);
            });

        return registry;
    }

    private static List<Type> GetEventHandlerTypes(Assembly[] assemblies)
    {
        return assemblies.Distinct()
            .SelectMany(GetTypesFromAssembly)
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(IsEventHandlerType)
            .ToList();
    }

    private static Type[] GetTypesFromAssembly(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Use only the types that could be loaded successfully
            return ex.Types.Where(t => t != null).ToArray()!;
        }
    }

    private static bool IsEventHandlerType(Type type)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType &&
                      (i.GetGenericTypeDefinition() == typeof(IEventHandler<>) ||
                       i.GetGenericTypeDefinition() == typeof(IAsyncEventHandler<>)));
    }
}