using System.Collections.Generic;
using System.Threading;

namespace DataStore;

/// <summary>
/// Simple in-memory key-value store for byte arrays with thread-safe operations.
/// </summary>
public class DataStore : IDisposable
{
    private readonly Dictionary<string, byte[]> _storage = [];
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private long _setCount = 0;
    private long _getCount = 0;
    private long _deleteCount = 0;

    /// <summary>
    /// Stores a value with the specified key.
    /// If the key already exists, the value is overwritten.
    /// </summary>
    /// <param name="key">The key to store the value under</param>
    /// <param name="value">The byte array value to store</param>
    public void Set(string key, byte[] value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value), "Value cannot be null");

        _lock.EnterWriteLock();
        try
        {
            _storage[key] = value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        Interlocked.Increment(ref _setCount);
    }

    /// <summary>
    /// Retrieves a value by key.
    /// </summary>
    /// <param name="key">The key to look up</param>
    /// <returns>The byte array value, or null if the key does not exist</returns>
    public byte[]? Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        byte[]? value;
        _lock.EnterReadLock();
        try
        {
            _storage.TryGetValue(key, out value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
        Interlocked.Increment(ref _getCount);
        return value;
    }

    /// <summary>
    /// Removes a key and its associated value from the store.
    /// </summary>
    /// <param name="key">The key to remove</param>
    /// <returns>True if the key was found and removed, false otherwise</returns>
    public bool Delete(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        bool removed;
        _lock.EnterWriteLock();
        try
        {
            removed = _storage.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        Interlocked.Increment(ref _deleteCount);
        return removed;
    }

    /// <summary>
    /// Checks if a key exists in the store.
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key exists, false otherwise</returns>
    public bool Contains(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        _lock.EnterReadLock();
        try
        {
            return _storage.ContainsKey(key);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Returns the number of key-value pairs in the store.
    /// </summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _storage.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Removes all key-value pairs from the store.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _storage.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets all keys currently in the store.
    /// </summary>
    /// <returns>An array of all keys</returns>
    public string[] GetAllKeys()
    {
        _lock.EnterReadLock();
        try
        {
            return _storage.Keys.ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the number of Set operations performed.
    /// </summary>
    public long SetCount => Interlocked.Read(ref _setCount);

    /// <summary>
    /// Gets the number of Get operations performed.
    /// </summary>
    public long GetCount => Interlocked.Read(ref _getCount);

    /// <summary>
    /// Gets the number of Delete operations performed.
    /// </summary>
    public long DeleteCount => Interlocked.Read(ref _deleteCount);

    /// <summary>
    /// Returns the number of Set, Get, and Delete operations performed.
    /// </summary>
    /// <returns></returns>
    public (long setCount, long getCount, long deleteCount) GetStatistics()
    {
        // to ensure thread-safe reading of statistics
        return (Interlocked.Read(ref _setCount),
                Interlocked.Read(ref _getCount),
                Interlocked.Read(ref _deleteCount));
    }

    /// <summary>
    /// Disposes the DataStore instance.
    /// </summary>
    public void Dispose()
    {
        _lock.Dispose();
    }
}