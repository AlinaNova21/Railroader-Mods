using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Serilog;
using Veldrid;
using AlinasRailTools.Shared.Data;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Resources;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that syncs ECS entities with the WorkingStateDB
/// Monitors database changes and creates/updates/deletes entities accordingly
/// </summary>
public class DatabaseSyncSystem : IDatabaseSyncSystem
{
    private readonly ILogger _logger;
    private readonly World _world;
    private readonly WorkingStateDB _db;
    private readonly MeshManager _meshManager;
    private readonly Guid _arrowMeshId;
    private readonly Material _arrowMaterial;

    // Track node ID -> entity
    private readonly Dictionary<string, Entity> _nodeEntities = new();

    // Track segment ID -> entity
    private readonly Dictionary<string, Entity> _segmentEntities = new();

    // Performance: Limit entity creation per frame to prevent freezing
    private readonly int _maxEntityCreationsPerFrame = 100;
    private int _entityCreationsThisFrame = 0;

    public DatabaseSyncSystem(World world, WorkingStateDB db, MeshManager meshManager, VeldridMesh arrowMesh)
    {
        _logger = Log.ForContext<DatabaseSyncSystem>();
        _world = world;
        _db = db;
        _meshManager = meshManager;

        // Register arrow mesh with manager
        _arrowMeshId = meshManager.RegisterMesh(arrowMesh);

        // Create red arrow material
        _arrowMaterial = Material.CreateColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), supportsInstancing: false);

        _logger.Information("Initialized");
    }

    /// <summary>
    /// Sync all dirty records from database to ECS
    /// </summary>
    public void Update(World world)
    {
        // Reset frame counter
        _entityCreationsThisFrame = 0;

        SyncNodes();
        SyncSegments();

        // Log if we hit the limit (means more work pending)
        if (_entityCreationsThisFrame >= _maxEntityCreationsPerFrame)
        {
            var nodeStats = _db.GetStats<TrackNodeData>();
            var segmentStats = _db.GetStats<TrackSegmentData>();
            _logger.Information("Created {Count} entities this frame (limit reached), {NodesTotal} nodes and {SegmentsTotal} segments in DB",
                _entityCreationsThisFrame, nodeStats.total, segmentStats.total);
        }
    }

    private void SyncNodes()
    {
        var dirtyNodes = _db.GetDirty<TrackNodeData>();

        foreach (var (id, node, isDeleted) in dirtyNodes)
        {
            // Limit entity creations per frame to prevent freezing
            if (_entityCreationsThisFrame >= _maxEntityCreationsPerFrame && !isDeleted)
                return; // Skip creations, but allow deletions to continue

            if (isDeleted)
            {
                // Delete entity if it exists
                if (_nodeEntities.TryGetValue(id.Id, out var entity))
                {
                    if (_world.IsAlive(entity))
                    {
                        // Remove mesh reference before destroying entity
                        _meshManager.RemoveReference(_arrowMeshId);
                        _world.Destroy(entity);
                    }
                    _nodeEntities.Remove(id.Id);
                }
                _db.ClearDirty(id);
                continue;
            }

            // Create or update entity
            if (_nodeEntities.TryGetValue(id.Id, out var existingEntity) && _world.IsAlive(existingEntity))
            {
                // Update existing entity
                if (_world.TryGet(existingEntity, out Transform transform))
                {
                    transform.Position = node.Position;

                    // Convert Euler angles to quaternion
                    float yawRad = node.Rotation.Y * MathF.PI / 180.0f;
                    transform.Rotation = Quaternion.CreateFromYawPitchRoll(yawRad, 0, 0);

                    _world.Set(existingEntity, transform);
                }
            }
            else
            {
                // Create new entity
                float yawRad = node.Rotation.Y * MathF.PI / 180.0f;
                var transform = new Transform(
                    node.Position,
                    Quaternion.CreateFromYawPitchRoll(yawRad, 0, 0),
                    Vector3.One
                );

                var meshRendererComp = new MeshRendererComponent(_arrowMeshId, _arrowMaterial);
                var trackNodeComp = new TrackNodeComponent(id.Id);

                var entity = _world.Create(transform, meshRendererComp, new RenderableComponent(), trackNodeComp);
                _nodeEntities[id.Id] = entity;

                // Add reference to mesh
                _meshManager.AddReference(_arrowMeshId);

                _entityCreationsThisFrame++;
            }

            _db.ClearDirty(id);
        }
    }

    private void SyncSegments()
    {
        var dirtySegments = _db.GetDirty<TrackSegmentData>();

        foreach (var (id, segment, isDeleted) in dirtySegments)
        {
            // Limit entity creations per frame to prevent freezing
            if (_entityCreationsThisFrame >= _maxEntityCreationsPerFrame && !isDeleted)
                return; // Skip creations, but allow deletions to continue

            if (isDeleted)
            {
                // Delete entity if it exists
                if (_segmentEntities.TryGetValue(id.Id, out var entity))
                {
                    if (_world.IsAlive(entity))
                    {
                        _world.Destroy(entity);
                    }
                    _segmentEntities.Remove(id.Id);
                }
                _db.ClearDirty(id);
                continue;
            }

            // Create or update segment entity
            if (!_nodeEntities.TryGetValue(segment.StartNodeId, out var startEntity) ||
                !_nodeEntities.TryGetValue(segment.EndNodeId, out var endEntity))
            {
                // Skip if nodes don't exist yet
                continue;
            }

            if (_segmentEntities.TryGetValue(id.Id, out var existingEntity) && _world.IsAlive(existingEntity))
            {
                // Update existing segment (update node references only if they changed)
                if (_world.TryGet(existingEntity, out TrackSegmentComponent segmentComp))
                {
                    // Only mark dirty if node entities actually changed
                    bool nodesChanged = segmentComp.StartNodeEntity != startEntity || segmentComp.EndNodeEntity != endEntity;

                    segmentComp.StartNodeEntity = startEntity;
                    segmentComp.EndNodeEntity = endEntity;

                    if (nodesChanged)
                    {
                        segmentComp.IsDirty = true;  // Mark for curve recalculation only if nodes changed
                    }

                    _world.Set(existingEntity, segmentComp);
                }
            }
            else
            {
                // Create new segment entity
                var segmentComp = new TrackSegmentComponent(id.Id, startEntity, endEntity);
                var curveComp = new BezierCurveComponent();

                var entity = _world.Create(segmentComp, curveComp);
                _segmentEntities[id.Id] = entity;

                _entityCreationsThisFrame++;
            }

            _db.ClearDirty(id);
        }
    }

    /// <summary>
    /// Get entity for a node ID (for external queries)
    /// </summary>
    public Entity? GetNodeEntity(string nodeId)
    {
        return _nodeEntities.TryGetValue(nodeId, out var entity) ? entity : null;
    }

    /// <summary>
    /// Get entity for a segment ID (for external queries)
    /// </summary>
    public Entity? GetSegmentEntity(string segmentId)
    {
        return _segmentEntities.TryGetValue(segmentId, out var entity) ? entity : null;
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
