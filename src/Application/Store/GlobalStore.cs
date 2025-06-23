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
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Set the value of the item in the store of type <typeparamref name="T"/>
    /// If the item is already set, it will be overwritten
    /// Use this only for the first time setting the value
    /// </summary>
    /// <param name="item"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    public void Set<T>(T item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        EnterWriteLock();
        try
        {
            _store[typeof(T)] = item;
        }
        finally
        {
            ExitWriteLock();
        }
    }

    /// <summary>
    /// Get the value of the item in the store of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? Get<T>()
    {
        EnterReadLock();
        try
        {
            if (_store.TryGetValue(typeof(T), out var value))
                return (T)value;
            else
                return default;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <summary>
    /// Try to get the value of the item in the store of type <typeparamref name="T"/>
    /// </summary>
    /// <param name="item"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGet<T>([NotNullWhen(true)] out T? item)
    {
        EnterReadLock();
        try
        {
            if (_store.TryGetValue(typeof(T), out var value))
            {
                item = (T)value;
                return true;
            }
            else
            {
                item = default;
                return false;
            }
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <summary>
    /// Try to remove the item from the store of type <typeparamref name="T"/>
    /// Use this method with caution as it will remove the item key from the store
    /// If the item key is removed, there will be no way to get the item back
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void Clear<T>()
    {
        EnterWriteLock();
        try
        {
            _store.TryRemove(typeof(T), out _);
        }
        finally
        {
            ExitWriteLock();
        }
    }

    private void EnterWriteLock() => _lock.EnterWriteLock();
    private void ExitWriteLock() => _lock.ExitWriteLock();
    private void EnterReadLock() => _lock.EnterReadLock();
    private void ExitReadLock() => _lock.ExitReadLock();
}