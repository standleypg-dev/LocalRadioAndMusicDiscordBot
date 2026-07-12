using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Application.Store;

/// <summary>
/// Since this is just a simple bot for single server
/// we can use a memory store to store the necessary data.
/// </summary>
public class GlobalStore
{
    private readonly ConcurrentDictionary<Type, object> _store = new();

    /// <summary>
    /// Set the value of the item in the store of type <typeparamref name="T"/>
    /// If the item is already set, it will be overwritten
    /// </summary>
    /// <param name="item"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    public void Set<T>(T item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        _store[typeof(T)] = item;
    }

    /// <summary>
    /// Get the value of the item in the store of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? Get<T>()
    {
        return _store.TryGetValue(typeof(T), out var value) ? (T)value : default;
    }

    /// <summary>
    /// Try to get the value of the item in the store of type <typeparamref name="T"/>
    /// </summary>
    /// <param name="item"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGet<T>([NotNullWhen(true)] out T? item)
    {
        if (_store.TryGetValue(typeof(T), out var value))
        {
            item = (T)value;
            return true;
        }

        item = default;
        return false;
    }

    /// <summary>
    /// Try to remove the item from the store of type <typeparamref name="T"/>
    /// Use this method with caution as it will remove the item key from the store
    /// If the item key is removed, there will be no way to get the item back
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void Clear<T>()
    {
        _store.TryRemove(typeof(T), out _);
    }
}
