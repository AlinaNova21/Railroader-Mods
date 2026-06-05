using System.Numerics;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Extensions;
using Serilog;
using Veldrid;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that renders all entities with Transform, MeshComponent, MaterialComponent, and RenderableComponent.
/// Handles per-object uniform updates and draw call submission.
///
/// Coordinate System Conversion:
/// - ECS components use Unity coordinates: +X right, +Y up, +Z forward
/// - Rendering uses Veldrid coordinates: +X right, +Y up, -Z forward
/// - This system applies the conversion at render time
/// </summary>
public class RenderSystem : ITrackRenderSystem
{
    private readonly ILogger _logger;
    private readonly GraphicsDevice _device;
    private readonly DeviceBuffer _uniformBuffer;
    private readonly Dictionary<Pipeline, ResourceSet> _resourceSets;

    /// <summary>
    /// Coordinate conversion matrix: Unity (+Z forward) to Veldrid (-Z forward)
    /// This flips the Z-axis to convert between coordinate systems
    /// </summary>
    private static readonly Matrix4x4 UnityToVeldridCoordinates = Matrix4x4.CreateScale(1, 1, -1);

    [StructLayout(LayoutKind.Sequential)]
    private struct ObjectUniforms
    {
        public Matrix4x4 MVP;
        public Vector4 Color;
    }

    public RenderSystem(GraphicsDevice device)
    {
        _logger = Log.ForContext<RenderSystem>();
        _device = device;
        _resourceSets = new Dictionary<Pipeline, ResourceSet>();

        // Create a shared uniform buffer for per-object data
        _uniformBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)Marshal.SizeOf<ObjectUniforms>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        _logger.Information("Initialized");
    }

    /// <summary>
    /// Render all renderable entities to the command list with frustum culling
    /// </summary>
    public void Render(World world, CommandList cl, Camera camera)
    {
        Matrix4x4 view = camera.GetViewMatrix();
        Matrix4x4 projection = camera.GetProjectionMatrix();
        Frustum frustum = camera.GetFrustum();

        int totalEntities = 0;
        int culledEntities = 0;

        // Create query for all renderable entities
        var query = new QueryDescription()
            .WithAll<Transform, MeshComponent, MaterialComponent, RenderableComponent>();

        // Process each renderable entity
        world.Query(in query, (Entity entity, ref Transform transform, ref MeshComponent meshComp, ref MaterialComponent matComp) =>
        {
            totalEntities++;

            // Frustum culling: skip entities with bounding boxes that are outside the frustum
            if (entity.Has<BoundingBoxComponent>())
            {
                var boundsComp = entity.Get<BoundingBoxComponent>();

                // Bounding box is in local space, transform to world space
                Vector3 worldMin = boundsComp.Bounds.Min + transform.Position;
                Vector3 worldMax = boundsComp.Bounds.Max + transform.Position;
                var worldBounds = new BoundingBox(worldMin, worldMax);

                // Convert bounds from Unity space to Veldrid space for frustum test
                var boundsVeldrid = worldBounds.ToVeldridSpace();
                if (!frustum.Intersects(boundsVeldrid))
                {
                    culledEntities++;
                    return; // Skip this entity
                }
            }

            // Calculate model matrix once (used for both meshes if rendering both)
            Matrix4x4 model = transform.GetModelMatrix();
            Matrix4x4 modelVeldrid = model * UnityToVeldridCoordinates;
            Matrix4x4 mvp = modelVeldrid * view * projection;

            // Get or create resource set for this pipeline
            if (!_resourceSets.TryGetValue(matComp.Pipeline, out var resourceSet))
            {
                resourceSet = _device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    matComp.UniformLayout,
                    _uniformBuffer));
                _resourceSets[matComp.Pipeline] = resourceSet;
            }

            // LOD: Determine which mesh(es) to render based on distance from camera to segment center
            bool hasLOD = entity.Has<TrackLODComponent>();

            if (hasLOD)
            {
                var lodComp = entity.Get<TrackLODComponent>();

                // Calculate distance from camera to this segment's center (transform position)
                float distance = Vector3.Distance(camera.Position, transform.Position);

                // Determine LOD state based on distance
                if (distance > TrackLODComponent.FadeEndDistance)
                {
                    // Far away: render simple mesh only
                    if (lodComp.SimpleMesh != null)
                    {
                        RenderMesh(cl, lodComp.SimpleMesh, mvp, matComp, resourceSet, 1.0f);
                    }
                }
                else if (distance > TrackLODComponent.FadeStartDistance)
                {
                    // In fade range: render BOTH meshes with crossfading alphas
                    float fadeProgress = (distance - TrackLODComponent.FadeStartDistance) /
                                       (TrackLODComponent.FadeEndDistance - TrackLODComponent.FadeStartDistance);

                    // Render detailed mesh fading out (alpha goes from 1.0 to 0.0)
                    if (meshComp.Mesh != null)
                    {
                        RenderMesh(cl, meshComp.Mesh, mvp, matComp, resourceSet, 1.0f - fadeProgress);
                    }

                    // Render simple mesh fading in (alpha goes from 0.0 to 1.0)
                    if (lodComp.SimpleMesh != null)
                    {
                        RenderMesh(cl, lodComp.SimpleMesh, mvp, matComp, resourceSet, fadeProgress);
                    }
                }
                else
                {
                    // Close: render detailed mesh only
                    if (meshComp.Mesh != null)
                    {
                        RenderMesh(cl, meshComp.Mesh, mvp, matComp, resourceSet, 1.0f);
                    }
                }
            }
            else
            {
                // No LOD: render mesh normally
                if (meshComp.Mesh != null)
                {
                    RenderMesh(cl, meshComp.Mesh, mvp, matComp, resourceSet, 1.0f);
                }
            }
        });

        // Uncomment for debugging culling performance
        // if (totalEntities > 0)
        //     Console.WriteLine($"[RenderSystem] Rendered {totalEntities - culledEntities}/{totalEntities} entities (culled {culledEntities})");
    }

    /// <summary>
    /// Helper method to render a single mesh with specified MVP and alpha
    /// </summary>
    private void RenderMesh(CommandList cl, VeldridMesh mesh, Matrix4x4 mvp, MaterialComponent matComp, ResourceSet resourceSet, float alpha)
    {
        // Update uniforms with alpha for fading
        Vector4 colorWithAlpha = new Vector4(matComp.Color.X, matComp.Color.Y, matComp.Color.Z, matComp.Color.W * alpha);
        ObjectUniforms uniforms = new ObjectUniforms
        {
            MVP = mvp,
            Color = colorWithAlpha
        };

        // Set pipeline and resources
        cl.SetPipeline(matComp.Pipeline);
        cl.UpdateBuffer(_uniformBuffer, 0, uniforms);
        cl.SetGraphicsResourceSet(0, resourceSet);

        // Set vertex and index buffers
        cl.SetVertexBuffer(0, mesh.VertexBuffer);
        if (mesh.HasIndices)
        {
            cl.SetIndexBuffer(mesh.IndexBuffer!, IndexFormat.UInt32);
        }

        // Draw
        if (mesh.HasIndices)
        {
            cl.DrawIndexed(mesh.IndexCount);
        }
        else
        {
            cl.Draw(mesh.VertexCount);
        }
    }

    public void Dispose()
    {
        _uniformBuffer?.Dispose();

        foreach (var resourceSet in _resourceSets.Values)
        {
            resourceSet?.Dispose();
        }
        _resourceSets.Clear();

        _logger.Information("Disposed");
    }
}
