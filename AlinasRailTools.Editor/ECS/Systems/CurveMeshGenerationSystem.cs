using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Serilog;
using Veldrid;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that generates rail meshes from bezier curves
/// Creates individual meshes per segment for dynamic updates
/// </summary>
public class CurveMeshGenerationSystem : ICurveMeshGenerationSystem
{
    private readonly ILogger _logger;
    private readonly GraphicsDevice _device;
    private readonly MeshManager _meshManager;
    private readonly Material _railMaterial;
    private readonly Material _tieMaterial;
    private readonly Guid _tieMeshId;
    private readonly float _railGauge = 1.44f; // Standard gauge (1.435m)
    private readonly float _railWidth = 0.15f; // Width of each rail
    private readonly float _railDepth = 0.15f; // Depth (height) of each rail
    private readonly float _railHeight = 0.05f; // Height above ground
    private readonly int _segmentsPerCurve = 30; // Subdivisions along curve (increased for smoother curves)
    private readonly Vector3 _silverColor = new Vector3(0.75f, 0.75f, 0.75f);

    // Tie parameters
    private readonly float _tieSpacing = 0.6f; // Distance between ties (meters)
    private readonly float _tieLength = 2.5f; // Length across rails (extends beyond gauge)
    private readonly float _tieWidth = 0.25f; // Width of tie
    private readonly float _tieHeight = 0.15f; // Height of tie

    // Performance: Limit mesh generation per frame to prevent freezing
    private readonly int _maxMeshGenerationsPerFrame = 50; // Process at most 50 segments per frame
    private int _meshGenerationsThisFrame = 0;
    private int _pendingMeshCount = 0; // Track pending meshes for UI display

    public CurveMeshGenerationSystem(GraphicsDevice device, MeshManager meshManager)
    {
        _logger = Log.ForContext<CurveMeshGenerationSystem>();
        _device = device;
        _meshManager = meshManager;

        // Create silver rail material (supports instancing for batch rendering)
        _railMaterial = Material.CreateColored(new Vector4(0.75f, 0.75f, 0.75f, 1.0f), supportsInstancing: true);

        // Create brown tie material (supports instancing)
        _tieMaterial = Material.CreateColored(new Vector4(0.4f, 0.3f, 0.2f, 1.0f), supportsInstancing: true);

        // Create single tie mesh (will be instanced for all ties)
        var tieMesh = CreateTieMesh();
        _tieMeshId = _meshManager.RegisterMesh(tieMesh);
        _meshManager.AddReference(_tieMeshId); // Keep a permanent reference

        _logger.Information("Initialized with tie mesh");
    }

    /// <summary>
    /// Update meshes for all segments with curves
    /// Regenerates mesh if curve has changed
    /// </summary>
    public void Update(World world)
    {
        // Reset frame counter
        _meshGenerationsThisFrame = 0;

        // Query all segments with curves but no mesh yet (exclude entities with LOD already configured)
        var newSegmentsQuery = new QueryDescription()
            .WithAll<TrackSegmentComponent, BezierCurveComponent>()
            .WithNone<LODComponent>();

        world.Query(in newSegmentsQuery, (Entity entity, ref TrackSegmentComponent segment, ref BezierCurveComponent curveComp) =>
        {
            // Limit mesh generation per frame to prevent freezing
            if (_meshGenerationsThisFrame >= _maxMeshGenerationsPerFrame)
                return;

            if (curveComp.Curve == null)
                return;

            // Generate rail mesh and tie transforms from curve
            var (mesh, tieTransforms, centerPoint) = GenerateRailMeshAndTies(curveComp.Curve);
            if (mesh == null)
                return;

            _meshGenerationsThisFrame++;

            // Register detailed mesh with manager and get ID
            Guid detailedMeshId = _meshManager.RegisterMesh(mesh);
            _meshManager.AddReference(detailedMeshId);

            // Add transform at segment center to parent entity
            world.Add(entity, new Transform(centerPoint, Quaternion.Identity, Vector3.One));

            // Create child entity for detailed mesh (LOD 0) with rails AND ties
            var detailedEntity = world.Create(
                new Transform(centerPoint, Quaternion.Identity, Vector3.One),
                new MeshRendererComponent(detailedMeshId, _railMaterial, lodLevel: 0),
                new BoundingBoxComponent(new BoundingBox(
                    curveComp.Curve.GetBounds().Min - centerPoint,
                    curveComp.Curve.GetBounds().Max - centerPoint).Expand(_tieLength / 2 + 1.0f))
            );

            // Add ties to the detailed LOD entity
            if (tieTransforms.Count > 0)
            {
                world.Add(detailedEntity, new MeshRendererListComponent(_tieMeshId, _tieMaterial, tieTransforms.ToArray(), lodLevel: 0));
            }

            // Generate and register simple line mesh for LOD
            var simpleMesh = GenerateSimpleLineMesh(curveComp.Curve, centerPoint);
            if (simpleMesh != null)
            {
                Guid simpleMeshId = _meshManager.RegisterMesh(simpleMesh);
                _meshManager.AddReference(simpleMeshId);

                // Create child entity for simple mesh (LOD 1) - enabled = false initially
                var simpleEntity = world.Create(
                    new Transform(centerPoint, Quaternion.Identity, Vector3.One),
                    new MeshRendererComponent(simpleMeshId, _railMaterial, lodLevel: 1) { Enabled = false },
                    new BoundingBoxComponent(new BoundingBox(
                        curveComp.Curve.GetBounds().Min - centerPoint,
                        curveComp.Curve.GetBounds().Max - centerPoint).Expand(_tieLength / 2 + 1.0f))
                );

                // Add LOD component to parent with references to child entities
                var lodComp = new LODComponent(
                    new[] { 1500.0f }, // Distance threshold: switch at 1.5km
                    new[] { detailedEntity, simpleEntity }
                );
                world.Add(entity, lodComp);
            }
            // If no simple mesh, just use detailed entity with no LOD switching
        });

        // Query dirty segments that already have meshes and regenerate them
        var dirtySegmentsQuery = new QueryDescription()
            .WithAll<TrackSegmentComponent, BezierCurveComponent, MeshRendererComponent>();

        world.Query(in dirtySegmentsQuery, (Entity entity, ref TrackSegmentComponent segment, ref BezierCurveComponent curveComp, ref MeshRendererComponent meshRendererComp) =>
        {
            // Limit mesh generation per frame to prevent freezing
            if (_meshGenerationsThisFrame >= _maxMeshGenerationsPerFrame)
                return;

            if (!segment.IsDirty || curveComp.Curve == null)
                return;

            _meshGenerationsThisFrame++;

            // Remove old mesh reference
            _meshManager.RemoveReference(meshRendererComp.MeshId);

            // Generate new rail mesh and tie transforms
            var (mesh, tieTransforms, centerPoint) = GenerateRailMeshAndTies(curveComp.Curve);
            if (mesh == null)
                return;

            // Register new mesh
            Guid newMeshId = _meshManager.RegisterMesh(mesh);
            _meshManager.AddReference(newMeshId);

            // Update mesh renderer component
            meshRendererComp.MeshId = newMeshId;
            world.Set(entity, meshRendererComp);

            // Update transform to new center point
            if (entity.Has<Transform>())
            {
                var transform = entity.Get<Transform>();
                transform.Position = centerPoint;
                world.Set(entity, transform);
            }
            else
            {
                world.Add(entity, new Transform(centerPoint, Quaternion.Identity, Vector3.One));
            }

            // Update or add tie transforms
            if (tieTransforms.Count > 0)
            {
                var tieRendererList = new MeshRendererListComponent(_tieMeshId, _tieMaterial, tieTransforms.ToArray());
                if (entity.Has<MeshRendererListComponent>())
                {
                    world.Set(entity, tieRendererList);
                }
                else
                {
                    world.Add(entity, tieRendererList);
                    // Also add RenderableComponent if not present
                    if (!entity.Has<RenderableComponent>())
                    {
                        world.Add(entity, new RenderableComponent());
                    }
                }
            }

            // Update bounding box (in local space)
            var worldBounds = curveComp.Curve.GetBounds();
            var localBounds = new BoundingBox(worldBounds.Min - centerPoint, worldBounds.Max - centerPoint);
            if (entity.Has<BoundingBoxComponent>())
            {
                world.Set(entity, new BoundingBoxComponent(localBounds.Expand(_tieLength / 2 + 1.0f)));
            }
            else
            {
                world.Add(entity, new BoundingBoxComponent(localBounds.Expand(_tieLength / 2 + 1.0f)));
            }

            // Clear dirty flag
            segment.IsDirty = false;
            world.Set(entity, segment);
        });

        // Log progress if we hit the limit (means there's more work to do next frame)
        if (_meshGenerationsThisFrame >= _maxMeshGenerationsPerFrame)
        {
            // Count remaining work
            int remainingNew = 0;
            world.Query(in newSegmentsQuery, (Entity e, ref TrackSegmentComponent s, ref BezierCurveComponent c) =>
            {
                if (c.Curve != null) remainingNew++;
            });

            int remainingDirty = 0;
            world.Query(in dirtySegmentsQuery, (Entity e, ref TrackSegmentComponent s, ref BezierCurveComponent c, ref MeshComponent m) =>
            {
                if (s.IsDirty && c.Curve != null) remainingDirty++;
            });

            if (remainingNew > 0 || remainingDirty > 0)
            {
                _logger.Information("Generated {MeshCount} meshes this frame, {RemainingCount} remaining...", _meshGenerationsThisFrame, remainingNew + remainingDirty);
            }

            _pendingMeshCount = remainingNew + remainingDirty;
        }
        else
        {
            _pendingMeshCount = 0;
        }
    }

    /// <summary>
    /// Get the number of segments pending mesh generation
    /// </summary>
    public int GetPendingMeshCount()
    {
        return _pendingMeshCount;
    }

    /// <summary>
    /// Generate rail mesh and tie transforms from bezier curve
    /// Creates two parallel extruded rails along the curve with proper volume
    /// Returns tie transforms for instanced rendering, plus the center point
    /// Mesh vertices are in local space relative to the returned center point
    /// </summary>
    private (VeldridMesh? mesh, List<Matrix4x4> tieTransforms, Vector3 centerPoint) GenerateRailMeshAndTies(BezierCurve curve)
    {
        var vertices = new List<VeldridMesh.ColorVertex>();
        var indices = new List<uint>();

        // First pass: calculate approximate curve length to determine tie placement
        float curveLength = 0f;
        Vector3 prevPos = curve.Evaluate(0);
        for (int i = 1; i <= _segmentsPerCurve; i++)
        {
            float t = i / (float)_segmentsPerCurve;
            Vector3 pos = curve.Evaluate(t);
            curveLength += Vector3.Distance(prevPos, pos);
            prevPos = pos;
        }

        // Generate rail geometry along the curve
        for (int i = 0; i <= _segmentsPerCurve; i++)
        {
            float t = i / (float)_segmentsPerCurve;
            Vector3 position = curve.Evaluate(t);
            Vector3 tangent = Vector3.Normalize(curve.GetTangent(t));

            // Always use horizontal plane for rails (ignore terrain banking)
            Vector3 horizontalTangent = new Vector3(tangent.X, 0, tangent.Z);
            if (horizontalTangent.Length() < 0.001f)
            {
                // Tangent is vertical, skip this segment
                continue;
            }
            horizontalTangent = Vector3.Normalize(horizontalTangent);

            // Right vector is perpendicular to tangent in the horizontal plane
            Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, horizontalTangent));

            // Create extruded rectangular cross-section for each rail
            // Left rail (4 vertices per cross-section: top-left, top-right, bottom-left, bottom-right)
            Vector3 leftRailCenter = position + right * (_railGauge / 2) + new Vector3(0, _railHeight, 0);
            uint leftRailStart = (uint)vertices.Count;

            // Top face vertices
            vertices.Add(new VeldridMesh.ColorVertex(leftRailCenter + new Vector3(-_railWidth/2, _railDepth/2, 0), _silverColor)); // top-left
            vertices.Add(new VeldridMesh.ColorVertex(leftRailCenter + new Vector3(_railWidth/2, _railDepth/2, 0), _silverColor));  // top-right

            // Bottom face vertices
            vertices.Add(new VeldridMesh.ColorVertex(leftRailCenter + new Vector3(-_railWidth/2, -_railDepth/2, 0), _silverColor)); // bottom-left
            vertices.Add(new VeldridMesh.ColorVertex(leftRailCenter + new Vector3(_railWidth/2, -_railDepth/2, 0), _silverColor));  // bottom-right

            // Right rail (4 vertices per cross-section)
            Vector3 rightRailCenter = position - right * (_railGauge / 2) + new Vector3(0, _railHeight, 0);
            uint rightRailStart = (uint)vertices.Count;

            // Top face vertices
            vertices.Add(new VeldridMesh.ColorVertex(rightRailCenter + new Vector3(-_railWidth/2, _railDepth/2, 0), _silverColor)); // top-left
            vertices.Add(new VeldridMesh.ColorVertex(rightRailCenter + new Vector3(_railWidth/2, _railDepth/2, 0), _silverColor));  // top-right

            // Bottom face vertices
            vertices.Add(new VeldridMesh.ColorVertex(rightRailCenter + new Vector3(-_railWidth/2, -_railDepth/2, 0), _silverColor)); // bottom-left
            vertices.Add(new VeldridMesh.ColorVertex(rightRailCenter + new Vector3(_railWidth/2, -_railDepth/2, 0), _silverColor));  // bottom-right

            // Connect to previous segment (create quads for all faces)
            if (i > 0)
            {
                // Each segment adds 8 vertices (4 per rail)
                // Previous segment vertices are 8 vertices back

                // Left rail extrusion
                uint prevLTL = leftRailStart - 8; // prev top-left
                uint prevLTR = leftRailStart - 7; // prev top-right
                uint prevLBL = leftRailStart - 6; // prev bottom-left
                uint prevLBR = leftRailStart - 5; // prev bottom-right

                uint currLTL = leftRailStart;     // curr top-left
                uint currLTR = leftRailStart + 1; // curr top-right
                uint currLBL = leftRailStart + 2; // curr bottom-left
                uint currLBR = leftRailStart + 3; // curr bottom-right

                // Top face
                indices.Add(prevLTL); indices.Add(currLTL); indices.Add(prevLTR);
                indices.Add(prevLTR); indices.Add(currLTL); indices.Add(currLTR);

                // Bottom face
                indices.Add(prevLBL); indices.Add(prevLBR); indices.Add(currLBL);
                indices.Add(prevLBR); indices.Add(currLBR); indices.Add(currLBL);

                // Left side face
                indices.Add(prevLTL); indices.Add(prevLBL); indices.Add(currLTL);
                indices.Add(prevLBL); indices.Add(currLBL); indices.Add(currLTL);

                // Right side face
                indices.Add(prevLTR); indices.Add(currLTR); indices.Add(prevLBR);
                indices.Add(prevLBR); indices.Add(currLTR); indices.Add(currLBR);

                // Right rail extrusion
                uint prevRTL = rightRailStart - 8; // prev top-left
                uint prevRTR = rightRailStart - 7; // prev top-right
                uint prevRBL = rightRailStart - 6; // prev bottom-left
                uint prevRBR = rightRailStart - 5; // prev bottom-right

                uint currRTL = rightRailStart;     // curr top-left
                uint currRTR = rightRailStart + 1; // curr top-right
                uint currRBL = rightRailStart + 2; // curr bottom-left
                uint currRBR = rightRailStart + 3; // curr bottom-right

                // Top face
                indices.Add(prevRTL); indices.Add(currRTL); indices.Add(prevRTR);
                indices.Add(prevRTR); indices.Add(currRTL); indices.Add(currRTR);

                // Bottom face
                indices.Add(prevRBL); indices.Add(prevRBR); indices.Add(currRBL);
                indices.Add(prevRBR); indices.Add(currRBR); indices.Add(currRBL);

                // Left side face
                indices.Add(prevRTL); indices.Add(prevRBL); indices.Add(currRTL);
                indices.Add(prevRBL); indices.Add(currRBL); indices.Add(currRTL);

                // Right side face
                indices.Add(prevRTR); indices.Add(currRTR); indices.Add(prevRBR);
                indices.Add(prevRBR); indices.Add(currRTR); indices.Add(currRBR);
            }
        }

        // Generate railroad tie transforms at regular intervals
        var tieTransforms = new List<Matrix4x4>();
        int numTies = (int)(curveLength / _tieSpacing);
        float distanceAccumulated = 0f;
        int nextTieIndex = 0;
        prevPos = curve.Evaluate(0);

        for (int i = 1; i <= _segmentsPerCurve; i++)
        {
            float t = i / (float)_segmentsPerCurve;
            Vector3 position = curve.Evaluate(t);
            float segmentLength = Vector3.Distance(prevPos, position);
            distanceAccumulated += segmentLength;

            // Check if we should place a tie at this segment
            while (nextTieIndex < numTies && (nextTieIndex * _tieSpacing) <= distanceAccumulated)
            {
                // Calculate exact position along curve for this tie
                float tieDistance = nextTieIndex * _tieSpacing;
                float tieT = t - ((distanceAccumulated - tieDistance) / segmentLength) * (1f / _segmentsPerCurve);
                tieT = Math.Clamp(tieT, 0f, 1f);

                Vector3 tiePos = curve.Evaluate(tieT);
                Vector3 tieTangent = Vector3.Normalize(curve.GetTangent(tieT));

                // Horizontal tangent and right vector
                Vector3 horizontalTangent = new Vector3(tieTangent.X, 0, tieTangent.Z);
                if (horizontalTangent.Length() > 0.001f)
                {
                    horizontalTangent = Vector3.Normalize(horizontalTangent);

                    // Position tie slightly below ground (keep in Unity space)
                    Vector3 tieCenter = tiePos + new Vector3(0, -_tieHeight / 2, 0);

                    // Create rotation matrix from basis vectors (Unity space, InstancedRenderSystem will convert to Veldrid)
                    Matrix4x4 rotation = Matrix4x4.CreateWorld(Vector3.Zero, horizontalTangent, Vector3.UnitY);

                    // Create translation (Unity space)
                    Matrix4x4 translation = Matrix4x4.CreateTranslation(tieCenter);

                    // Combine: rotation then translation
                    Matrix4x4 transform = rotation * translation;

                    tieTransforms.Add(transform);
                }

                nextTieIndex++;
            }

            prevPos = position;
        }

        if (vertices.Count == 0)
            return (null, tieTransforms, Vector3.Zero);

        // Calculate center point (average of all vertex positions)
        Vector3 centerPoint = Vector3.Zero;
        foreach (var vertex in vertices)
        {
            centerPoint += vertex.Position;
        }
        centerPoint /= vertices.Count;

        // Make all vertices relative to center point (convert to local space)
        for (int i = 0; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
            vertex.Position -= centerPoint;
            vertices[i] = vertex;
        }

        // Make tie transforms relative to center point (stay in Unity space, InstancedRenderSystem will convert)
        for (int i = 0; i < tieTransforms.Count; i++)
        {
            // Extract position from transform (last column in row-major Matrix4x4)
            var worldPos = new Vector3(tieTransforms[i].M41, tieTransforms[i].M42, tieTransforms[i].M43);

            // Make position relative to center point (both in Unity space)
            Vector3 localPos = worldPos - centerPoint;

            // Update transform with local position (keep rotation unchanged)
            tieTransforms[i] = new Matrix4x4(
                tieTransforms[i].M11, tieTransforms[i].M12, tieTransforms[i].M13, tieTransforms[i].M14,
                tieTransforms[i].M21, tieTransforms[i].M22, tieTransforms[i].M23, tieTransforms[i].M24,
                tieTransforms[i].M31, tieTransforms[i].M32, tieTransforms[i].M33, tieTransforms[i].M34,
                localPos.X, localPos.Y, localPos.Z, tieTransforms[i].M44
            );
        }

        return (VeldridMesh.CreateColored(_device, vertices.ToArray(), indices.ToArray()), tieTransforms, centerPoint);
    }

    /// <summary>
    /// Create a rotation matrix from a forward vector (assumes Y-up)
    /// </summary>
    private Matrix4x4 CreateRotationFromForward(Vector3 forward)
    {
        Vector3 up = Vector3.UnitY;
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
        Vector3 actualUp = Vector3.Cross(forward, right);

        // Build rotation matrix from basis vectors (row-major order for C#)
        return new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            actualUp.X, actualUp.Y, actualUp.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1
        );
    }

    /// <summary>
    /// Generate a simple line mesh for distant LOD
    /// Single silver line down the center of the track
    /// Uses the SAME centerPoint as detailed mesh for consistent transforms
    /// </summary>
    private VeldridMesh? GenerateSimpleLineMesh(BezierCurve curve, Vector3 centerPoint)
    {
        var vertices = new List<VeldridMesh.ColorVertex>();
        var indices = new List<uint>();

        const int segments = 10; // Fewer segments for simple line
        float lineWidth = _railGauge; // Width of the silver bar (same as rail gauge)

        // Generate vertices along the curve (in world space first)
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 position = curve.Evaluate(t);
            Vector3 tangent = Vector3.Normalize(curve.GetTangent(t));

            // Calculate right vector (perpendicular to tangent on XZ plane)
            Vector3 horizontalTangent = new Vector3(tangent.X, 0, tangent.Z);
            if (horizontalTangent.Length() < 0.001f)
                continue;

            horizontalTangent = Vector3.Normalize(horizontalTangent);
            Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, horizontalTangent));

            // Create a horizontal bar at rail height + 3m offset (in world space)
            Vector3 centerPos = position + new Vector3(0, _railHeight + 3.0f, 0);

            // Two vertices for the width of the bar (world space)
            uint vertexStart = (uint)vertices.Count;
            vertices.Add(new VeldridMesh.ColorVertex(centerPos - right * (lineWidth / 2), _silverColor));
            vertices.Add(new VeldridMesh.ColorVertex(centerPos + right * (lineWidth / 2), _silverColor));

            // Create quad between this segment and the previous one
            if (i > 0)
            {
                uint prevLeft = vertexStart - 2;
                uint prevRight = vertexStart - 1;
                uint currLeft = vertexStart;
                uint currRight = vertexStart + 1;

                // Two triangles forming a quad (counter-clockwise winding for front-face)
                indices.Add(prevLeft); indices.Add(prevRight); indices.Add(currLeft);
                indices.Add(currLeft); indices.Add(prevRight); indices.Add(currRight);
            }
        }

        if (vertices.Count == 0)
            return null;

        // Convert all vertices to local space (relative to centerPoint) - same as detailed mesh
        for (int i = 0; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
            vertex.Position -= centerPoint;
            vertices[i] = vertex;
        }

        return VeldridMesh.CreateColored(_device, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Create a single tie mesh (rectangular box centered at origin)
    /// This mesh will be instanced for all ties
    /// </summary>
    private VeldridMesh CreateTieMesh()
    {
        var vertices = new List<VeldridMesh.ColorVertex>();
        var indices = new List<uint>();
        Vector3 tieColor = new Vector3(0.4f, 0.3f, 0.2f);

        // 8 vertices for rectangular box (centered at origin)
        // Bottom face (4 vertices)
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(-_tieLength/2, -_tieHeight/2, -_tieWidth/2), tieColor)); // 0
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(+_tieLength/2, -_tieHeight/2, -_tieWidth/2), tieColor)); // 1
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(+_tieLength/2, -_tieHeight/2, +_tieWidth/2), tieColor)); // 2
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(-_tieLength/2, -_tieHeight/2, +_tieWidth/2), tieColor)); // 3

        // Top face (4 vertices)
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(-_tieLength/2, +_tieHeight/2, -_tieWidth/2), tieColor)); // 4
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(+_tieLength/2, +_tieHeight/2, -_tieWidth/2), tieColor)); // 5
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(+_tieLength/2, +_tieHeight/2, +_tieWidth/2), tieColor)); // 6
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(-_tieLength/2, +_tieHeight/2, +_tieWidth/2), tieColor)); // 7

        // Generate indices for all 6 faces
        // Bottom face
        indices.Add(0); indices.Add(2); indices.Add(1);
        indices.Add(0); indices.Add(3); indices.Add(2);

        // Top face
        indices.Add(4); indices.Add(5); indices.Add(6);
        indices.Add(4); indices.Add(6); indices.Add(7);

        // Front face
        indices.Add(0); indices.Add(1); indices.Add(5);
        indices.Add(0); indices.Add(5); indices.Add(4);

        // Back face
        indices.Add(3); indices.Add(7); indices.Add(6);
        indices.Add(3); indices.Add(6); indices.Add(2);

        // Left face
        indices.Add(0); indices.Add(4); indices.Add(7);
        indices.Add(0); indices.Add(7); indices.Add(3);

        // Right face
        indices.Add(1); indices.Add(2); indices.Add(6);
        indices.Add(1); indices.Add(6); indices.Add(5);

        return VeldridMesh.CreateColored(_device, vertices.ToArray(), indices.ToArray());
    }

    public void Dispose()
    {
        // Release tie mesh reference
        _meshManager.RemoveReference(_tieMeshId);

        _logger.Information("Disposed");
    }
}
