using Veldrid;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Central manager for all VeldridMesh instances
/// Handles VRAM lifecycle, reference counting, and mesh registration
/// </summary>
public class MeshManager : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly Dictionary<Guid, VeldridMesh> _meshes;
    private readonly object _lock = new();

    public MeshManager(GraphicsDevice device)
    {
        _device = device;
        _meshes = new Dictionary<Guid, VeldridMesh>();
        Console.WriteLine("[MeshManager] Initialized");
    }

    /// <summary>
    /// Register a mesh with the manager
    /// The mesh will be tracked and automatically disposed when no longer referenced
    /// </summary>
    public Guid RegisterMesh(VeldridMesh mesh)
    {
        lock (_lock)
        {
            if (_meshes.ContainsKey(mesh.Id))
            {
                Console.WriteLine($"[MeshManager] Warning: Mesh {mesh.Id} already registered");
                return mesh.Id;
            }

            _meshes[mesh.Id] = mesh;
            Console.WriteLine($"[MeshManager] Registered mesh {mesh.Id} (total: {_meshes.Count})");
            return mesh.Id;
        }
    }

    /// <summary>
    /// Get a mesh by its unique ID
    /// Returns null if mesh not found
    /// </summary>
    public VeldridMesh? GetMesh(Guid id)
    {
        lock (_lock)
        {
            return _meshes.TryGetValue(id, out var mesh) ? mesh : null;
        }
    }

    /// <summary>
    /// Add a reference to a mesh
    /// Must be called when a component starts using the mesh
    /// </summary>
    public void AddReference(Guid meshId)
    {
        lock (_lock)
        {
            if (_meshes.TryGetValue(meshId, out var mesh))
            {
                mesh.AddReference();
                Console.WriteLine($"[MeshManager] Mesh {meshId} ref count: {mesh.ReferenceCount}");
            }
            else
            {
                Console.WriteLine($"[MeshManager] Warning: Attempted to add reference to unknown mesh {meshId}");
            }
        }
    }

    /// <summary>
    /// Remove a reference from a mesh
    /// Must be called when a component stops using the mesh
    /// Automatically disposes and unregisters the mesh if reference count reaches zero
    /// </summary>
    public void RemoveReference(Guid meshId)
    {
        lock (_lock)
        {
            if (_meshes.TryGetValue(meshId, out var mesh))
            {
                bool shouldDispose = mesh.RemoveReference();
                Console.WriteLine($"[MeshManager] Mesh {meshId} ref count: {mesh.ReferenceCount}");

                if (shouldDispose)
                {
                    Console.WriteLine($"[MeshManager] Disposing mesh {meshId} (ref count reached 0)");
                    mesh.Dispose();
                    _meshes.Remove(meshId);
                }
            }
            else
            {
                Console.WriteLine($"[MeshManager] Warning: Attempted to remove reference from unknown mesh {meshId}");
            }
        }
    }

    /// <summary>
    /// Unregister and dispose a mesh immediately, regardless of reference count
    /// Use with caution - this will invalidate any components still referencing the mesh
    /// </summary>
    public void UnregisterMesh(Guid meshId)
    {
        lock (_lock)
        {
            if (_meshes.TryGetValue(meshId, out var mesh))
            {
                if (mesh.ReferenceCount > 0)
                {
                    Console.WriteLine($"[MeshManager] Warning: Force-unregistering mesh {meshId} with {mesh.ReferenceCount} active references");
                }

                mesh.Dispose();
                _meshes.Remove(meshId);
                Console.WriteLine($"[MeshManager] Unregistered mesh {meshId} (total: {_meshes.Count})");
            }
        }
    }

    /// <summary>
    /// Get count of currently managed meshes
    /// </summary>
    public int MeshCount
    {
        get
        {
            lock (_lock)
            {
                return _meshes.Count;
            }
        }
    }

    /// <summary>
    /// Get total VRAM usage estimate (sum of all vertex and index buffer sizes)
    /// This is an approximation based on vertex/index counts
    /// </summary>
    public long EstimateVRAMUsage()
    {
        lock (_lock)
        {
            long total = 0;
            foreach (var mesh in _meshes.Values)
            {
                // Estimate buffer sizes (this is approximate)
                // TerrainVertex = 32 bytes, ColorVertex = 24 bytes
                // For simplicity, we'll use 32 bytes as upper bound
                total += mesh.VertexCount * 32;
                if (mesh.HasIndices)
                {
                    total += mesh.IndexCount * sizeof(uint);
                }
            }
            return total;
        }
    }

    /// <summary>
    /// Dispose all meshes and clear the registry
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            Console.WriteLine($"[MeshManager] Disposing {_meshes.Count} meshes");

            foreach (var mesh in _meshes.Values)
            {
                if (mesh.ReferenceCount > 0)
                {
                    Console.WriteLine($"[MeshManager] Warning: Disposing mesh {mesh.Id} with {mesh.ReferenceCount} active references");
                }
                mesh.Dispose();
            }

            _meshes.Clear();
            Console.WriteLine("[MeshManager] Disposed");
        }
    }
}
