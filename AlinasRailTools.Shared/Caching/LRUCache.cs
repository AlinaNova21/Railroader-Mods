using System;
using System.Collections.Generic;

namespace AlinasRailTools.Shared.Caching;

/// <summary>
/// Simple LRU (Least Recently Used) cache implementation.
/// Thread-safe for concurrent access.
/// </summary>
public class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly object _lock = new object();

    public LRUCache(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));
        }

        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        _lruList = new LinkedList<CacheItem>();
    }

    /// <summary>
    /// Try to get a value from the cache.
    /// Updates the item's position to most recently used if found.
    /// </summary>
    public bool TryGet(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);

                value = node.Value.Value;
                return true;
            }

            value = default!;
            return false;
        }
    }

    /// <summary>
    /// Add or update an item in the cache.
    /// If cache is full, removes the least recently used item.
    /// </summary>
    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            // If already exists, update and move to front
            if (_cache.TryGetValue(key, out var existingNode))
            {
                _lruList.Remove(existingNode);
                _lruList.AddFirst(existingNode);
                existingNode.Value.Value = value;
                return;
            }

            // Evict least recently used if at capacity
            if (_cache.Count >= _capacity)
            {
                var lruNode = _lruList.Last;
                if (lruNode != null)
                {
                    _lruList.RemoveLast();
                    _cache.Remove(lruNode.Value.Key);
                }
            }

            // Add new item to front
            var newItem = new CacheItem { Key = key, Value = value };
            var newNode = new LinkedListNode<CacheItem>(newItem);
            _lruList.AddFirst(newNode);
            _cache[key] = newNode;
        }
    }

    /// <summary>
    /// Remove an item from the cache.
    /// </summary>
    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _cache.Remove(key);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Clear all items from the cache.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
        }
    }

    /// <summary>
    /// Get the current number of items in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    private class CacheItem
    {
        public TKey Key { get; set; } = default!;
        public TValue Value { get; set; } = default!;
    }
}
