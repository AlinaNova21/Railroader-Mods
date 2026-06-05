using System;
using System.Collections.Generic;
using System.Linq;
using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.Shared.Data;

/// <summary>
/// In-memory NoSQL-style database for managing game state with change tracking.
/// Stores records as JObjects internally for flexible partial updates.
/// Thread-safe for concurrent access.
/// </summary>
public class WorkingStateDB
{
    // Collections: TypeName -> (Id -> JObject)
    private readonly Dictionary<string, Dictionary<string, JObject>> _collections = new();

    // Metadata: TypeName -> (Id -> Metadata)
    private readonly Dictionary<string, Dictionary<string, RecordMetadata>> _metadata = new();

    // Lock for thread-safe access
    private readonly object _lock = new();

    /// <summary>
    /// Get or create a collection for the specified type
    /// </summary>
    private Dictionary<string, JObject> GetCollection(string typeName)
    {
        if (!_collections.TryGetValue(typeName, out var collection))
        {
            collection = new Dictionary<string, JObject>();
            _collections[typeName] = collection;
            _metadata[typeName] = new Dictionary<string, RecordMetadata>();
        }
        return collection;
    }

    /// <summary>
    /// Get or create metadata collection for the specified type
    /// </summary>
    private Dictionary<string, RecordMetadata> GetMetadata(string typeName)
    {
        if (!_metadata.TryGetValue(typeName, out var meta))
        {
            meta = new Dictionary<string, RecordMetadata>();
            _metadata[typeName] = meta;
        }
        return meta;
    }

    /// <summary>
    /// Create a new record. Throws if record already exists.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if record with the same ID already exists</exception>
    public void Create<T>(ResId<T> id, T obj) where T : class
    {
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);
            var metadata = GetMetadata(typeName);

            if (collection.ContainsKey(id.Id))
                throw new InvalidOperationException($"Record {id} already exists in collection {typeName}");

            var jobj = JObject.FromObject(obj);
            collection[id.Id] = jobj;
            metadata[id.Id] = new RecordMetadata
            {
                IsDirty = true,
                IsDeleted = false,
                Source = RecordSource.Created
            };
        }
    }

    /// <summary>
    /// Get a record by ID. Throws if not found.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if record is not found</exception>
    public T Get<T>(ResId<T> id) where T : class
    {
        JObject jobj;
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);

            if (!collection.TryGetValue(id.Id, out jobj))
                throw new KeyNotFoundException($"Record {id} not found in collection {typeName}");
        }

        // Deserialize outside lock
        return jobj.ToObject<T>()!;
    }

    /// <summary>
    /// Try to get a record by ID. Returns false if not found.
    /// </summary>
    public bool TryGet<T>(ResId<T> id, out T result) where T : class
    {
        JObject jobj;
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);

            if (!collection.TryGetValue(id.Id, out jobj))
            {
                result = null;
                return false;
            }
        }

        // Deserialize outside lock
        result = jobj.ToObject<T>();
        return true;
    }

    /// <summary>
    /// List all records of the specified type
    /// </summary>
    public List<T> List<T>() where T : class
    {
        // Snapshot JObjects inside lock (cheap)
        List<JObject> snapshot;
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);
            snapshot = collection.Values.ToList();
        }

        // Deserialize outside lock (expensive)
        return snapshot
            .Select(jobj => jobj.ToObject<T>()!)
            .ToList();
    }

    /// <summary>
    /// List records that match the specified predicate
    /// </summary>
    public List<T> List<T>(Func<T, bool> predicate) where T : class
    {
        return List<T>().Where(predicate).ToList();
    }

    /// <summary>
    /// Upsert a record using a partial JObject patch.
    /// If record exists, merges the patch. If not, creates a new record.
    /// Marks record as dirty.
    /// </summary>
    public void Upsert<T>(ResId<T> id, JObject patch) where T : class
    {
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);
            var metadata = GetMetadata(typeName);

            if (collection.TryGetValue(id.Id, out var existing))
            {
                // Merge patch into existing
                existing.Merge(patch, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace,
                    MergeNullValueHandling = MergeNullValueHandling.Ignore
                });

                if (metadata.TryGetValue(id.Id, out var meta))
                {
                    meta.IsDirty = true;
                    meta.Source = RecordSource.FromJson;
                }
            }
            else
            {
                // Create new record from patch
                collection[id.Id] = patch;
                metadata[id.Id] = new RecordMetadata
                {
                    IsDirty = true,
                    IsDeleted = false,
                    Source = RecordSource.Created
                };
            }
        }
    }

    /// <summary>
    /// Replace an entire record (no merge). Marks as dirty.
    /// </summary>
    public void Replace<T>(ResId<T> id, T obj) where T : class
    {
        // Convert to JObject outside the lock (this is expensive)
        var jobj = JObject.FromObject(obj);

        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);
            var metadata = GetMetadata(typeName);

            collection[id.Id] = jobj;

            if (!metadata.TryGetValue(id.Id, out var meta))
            {
                meta = new RecordMetadata();
                metadata[id.Id] = meta;
            }

            meta.IsDirty = true;
            meta.Source = RecordSource.FromJson;
            meta.IsDeleted = false;
        }
    }

    /// <summary>
    /// Replace multiple records in a batch. More efficient than calling Replace() in a loop.
    /// </summary>
    public void ReplaceBatch<T>(IEnumerable<(ResId<T> id, T obj)> records) where T : class
    {
        // Convert all objects to JObjects outside the lock (expensive)
        var jobjects = records.Select(r => (r.id, jobj: JObject.FromObject(r.obj))).ToList();

        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);
            var metadata = GetMetadata(typeName);

            foreach (var (id, jobj) in jobjects)
            {
                collection[id.Id] = jobj;

                if (!metadata.TryGetValue(id.Id, out var meta))
                {
                    meta = new RecordMetadata();
                    metadata[id.Id] = meta;
                }

                meta.IsDirty = true;
                meta.Source = RecordSource.FromJson;
                meta.IsDeleted = false;
            }
        }
    }

    /// <summary>
    /// Mark a record for deletion. Marks as dirty.
    /// </summary>
    public void Delete<T>(ResId<T> id) where T : class
    {
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var metadata = GetMetadata(typeName);

            if (metadata.TryGetValue(id.Id, out var meta))
            {
                meta.IsDeleted = true;
                meta.IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Check if a record exists
    /// </summary>
    public bool Exists<T>(ResId<T> id) where T : class
    {
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);
            return collection.ContainsKey(id.Id);
        }
    }

    /// <summary>
    /// Get all dirty records of the specified type
    /// Returns an immutable snapshot - safe to enumerate outside the lock
    /// </summary>
    public IReadOnlyList<(ResId<T> id, T obj, bool isDeleted)> GetDirty<T>() where T : class
    {
        // Snapshot dirty records inside lock (just IDs and JObjects - cheap)
        List<(string id, JObject jobj, bool isDeleted)> snapshot;
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);
            var metadata = GetMetadata(typeName);

            // Materialize IDs and JObject references inside lock
            snapshot = metadata
                .Where(kvp => kvp.Value.IsDirty)
                .Select(kvp => (
                    id: kvp.Key,
                    jobj: collection[kvp.Key],
                    isDeleted: kvp.Value.IsDeleted
                ))
                .ToList();
        }

        // Deserialize JObjects to typed objects outside lock (expensive)
        return snapshot
            .Select(s => (
                id: new ResId<T>(s.id),
                obj: s.jobj.ToObject<T>()!,
                isDeleted: s.isDeleted
            ))
            .ToList()
            .AsReadOnly(); // Return as read-only to signal immutability
    }

    /// <summary>
    /// Clear the dirty flag for a record
    /// </summary>
    public void ClearDirty<T>(ResId<T> id) where T : class
    {
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var metadata = GetMetadata(typeName);

            if (metadata.TryGetValue(id.Id, out var meta))
            {
                meta.IsDirty = false;
            }
        }
    }

    /// <summary>
    /// Get the raw JObject for a record (advanced usage)
    /// </summary>
    public JObject GetRaw<T>(ResId<T> id) where T : class
    {
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var collection = GetCollection(typeName);
            return collection.TryGetValue(id.Id, out var jobj) ? jobj : null;
        }
    }

    /// <summary>
    /// Clear all records of the specified type
    /// </summary>
    public void Clear<T>() where T : class
    {
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            _collections.Remove(typeName);
            _metadata.Remove(typeName);
        }
    }

    /// <summary>
    /// Get statistics about the specified collection
    /// </summary>
    public (int total, int dirty, int deleted) GetStats<T>() where T : class
    {
        lock (_lock)
        {
            var typeName = typeof(T).Name;
            var metadata = GetMetadata(typeName);

            return (
                total: metadata.Count,
                dirty: metadata.Count(m => m.Value.IsDirty),
                deleted: metadata.Count(m => m.Value.IsDeleted)
            );
        }
    }

    /// <summary>
    /// Internal metadata for tracking record state
    /// </summary>
    internal class RecordMetadata
    {
        public bool IsDirty { get; set; }
        public bool IsDeleted { get; set; }
        public RecordSource Source { get; set; }
    }
}
