using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Extensions;
using Serilog;
using Veldrid;
using Veldrid.SPIRV;
using AlinasRailTools.Editor.ECS.Components;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Systems;

/// <summary>
/// System that automatically batches entities with the same mesh and material for instanced rendering
/// Groups entities by (MeshId, Material) and renders them in a single draw call per group
/// Only handles materials that support instancing
/// </summary>
public class InstancedRenderSystem : IInstancedRenderSystem
{
    private readonly ILogger _logger;
    private readonly GraphicsDevice _device;
    private readonly MeshManager _meshManager;
    private readonly OutputDescription _outputDescription;
    private readonly Dictionary<Material, PipelineResources> _pipelineCache;

    // Uniform buffer for view-projection matrix (shared across all instances)
    private readonly DeviceBuffer _uniformBuffer;

    // Single reusable instance buffer (grows as needed)
    private DeviceBuffer? _instanceBuffer = null;
    private uint _instanceBufferCapacity = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct InstanceUniforms
    {
        public Matrix4x4 ViewProjection;
        public Vector4 Color;
    }

    private struct PipelineResources
    {
        public Pipeline Pipeline;
        public ResourceLayout UniformLayout;
        public ResourceSet ResourceSet;
    }

    // Group of instances sharing the same mesh and material
    private struct InstanceGroup
    {
        public Guid MeshId;
        public Material Material;
        public List<Matrix4x4> Transforms;
        public BoundingBox? GroupBounds; // Combined bounds for culling
    }

    public InstancedRenderSystem(GraphicsDevice device, MeshManager meshManager, OutputDescription outputDescription)
    {
        _logger = Log.ForContext<InstancedRenderSystem>();
        _device = device;
        _meshManager = meshManager;
        _outputDescription = outputDescription;
        _pipelineCache = new Dictionary<Material, PipelineResources>();

        // Create shared uniform buffer
        _uniformBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)Marshal.SizeOf<InstanceUniforms>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        _logger.Information("Initialized");
    }

    /// <summary>
    /// Render all entities that support instancing, grouped by mesh+material
    /// </summary>
    public void Render(World world, CommandList cl, Camera camera)
    {
        Matrix4x4 view = camera.GetViewMatrix();
        Matrix4x4 projection = camera.GetProjectionMatrix();
        Matrix4x4 viewProjection = view * projection;
        Frustum frustum = camera.GetFrustum();

        // Group entities by (MeshId, Material) for instancing
        var instanceGroups = new Dictionary<(Guid, Material), InstanceGroup>();

        // Process entities with single MeshRendererComponent
        var singleRendererQuery = new QueryDescription()
            .WithAll<Transform, MeshRendererComponent>();

        world.Query(in singleRendererQuery, (Entity entity, ref Transform transform, ref MeshRendererComponent renderer) =>
        {
            ProcessRenderer(entity, ref transform, ref renderer, camera, frustum, instanceGroups);
        });

        // Process entities with MeshRendererCollectionComponent
        var collectionQuery = new QueryDescription()
            .WithAll<Transform, MeshRendererCollectionComponent, RenderableComponent>();

        world.Query(in collectionQuery, (Entity entity, ref Transform transform, ref MeshRendererCollectionComponent collection) =>
        {
            // Process each enabled renderer in the collection
            foreach (var renderer in collection.Renderers)
            {
                if (renderer.Enabled)
                {
                    var mutableRenderer = renderer; // Make a mutable copy
                    ProcessRenderer(entity, ref transform, ref mutableRenderer, camera, frustum, instanceGroups);
                }
            }
        });

        // Process entities with MeshRendererListComponent (multiple instances per entity)
        var listQuery = new QueryDescription()
            .WithAll<Transform, MeshRendererListComponent>();

        world.Query(in listQuery, (Entity entity, ref Transform transform, ref MeshRendererListComponent listRenderer) =>
        {
            ProcessRendererList(entity, ref transform, ref listRenderer, camera, frustum, instanceGroups);
        });

        // Render each instance group
        foreach (var kvp in instanceGroups)
        {
            var group = kvp.Value;

            // Skip empty groups
            if (group.Transforms.Count == 0)
                continue;

            // Get mesh from manager
            var mesh = _meshManager.GetMesh(group.MeshId);
            if (mesh == null)
            {
                _logger.Warning("Mesh {MeshId} not found in MeshManager", group.MeshId);
                continue;
            }

            // Get or create pipeline for this material
            if (!_pipelineCache.TryGetValue(group.Material, out var pipelineRes))
            {
                pipelineRes = CreatePipeline(group.Material);
                _pipelineCache[group.Material] = pipelineRes;
            }

            // Update uniforms
            InstanceUniforms uniforms = new InstanceUniforms
            {
                ViewProjection = viewProjection,
                Color = group.Material.Color
            };
            cl.UpdateBuffer(_uniformBuffer, 0, uniforms);

            // Set pipeline and resources
            cl.SetPipeline(pipelineRes.Pipeline);
            cl.SetGraphicsResourceSet(0, pipelineRes.ResourceSet);

            // Set vertex buffer
            cl.SetVertexBuffer(0, mesh.VertexBuffer);
            if (mesh.HasIndices)
            {
                cl.SetIndexBuffer(mesh.IndexBuffer!, IndexFormat.UInt32);
            }

            // Ensure instance buffer is large enough (grow if needed)
            uint requiredSize = (uint)(group.Transforms.Count * Marshal.SizeOf<Matrix4x4>());
            if (_instanceBuffer == null || _instanceBufferCapacity < requiredSize)
            {
                // Dispose old buffer if it exists
                _instanceBuffer?.Dispose();

                // Create new buffer with 50% headroom to reduce future reallocations
                _instanceBufferCapacity = (uint)(requiredSize * 1.5f);
                _instanceBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(
                    _instanceBufferCapacity,
                    BufferUsage.VertexBuffer | BufferUsage.Dynamic));

                uint maxInstances = _instanceBufferCapacity / (uint)Marshal.SizeOf<Matrix4x4>();
                _logger.Information("Resized instance buffer: capacity={CapacityBytes} bytes (max {MaxInstances} instances)",
                    _instanceBufferCapacity, maxInstances);
            }

            // Reuse the same buffer for all groups (GPU consumes data before next draw)
            cl.UpdateBuffer(_instanceBuffer, 0, group.Transforms.ToArray());
            cl.SetVertexBuffer(1, _instanceBuffer);

            // Draw all instances in one call
            if (mesh.HasIndices)
            {
                cl.DrawIndexed(
                    indexCount: mesh.IndexCount,
                    instanceCount: (uint)group.Transforms.Count,
                    indexStart: 0,
                    vertexOffset: 0,
                    instanceStart: 0);
            }
            else
            {
                cl.Draw(
                    vertexCount: mesh.VertexCount,
                    instanceCount: (uint)group.Transforms.Count,
                    vertexStart: 0,
                    instanceStart: 0);
            }
        }
    }

    /// <summary>
    /// Process a single renderer for instancing
    /// </summary>
    private void ProcessRenderer(Entity entity, ref Transform transform, ref MeshRendererComponent renderer,
        Camera camera, Frustum frustum, Dictionary<(Guid, Material), InstanceGroup> instanceGroups)
    {
        // Skip disabled renderers
        if (!renderer.Enabled)
            return;

        // Only process materials that support instancing
        if (!renderer.Material.SupportsInstancing)
            return;

        // Frustum culling
        if (entity.Has<BoundingBoxComponent>())
        {
            var boundsComp = entity.Get<BoundingBoxComponent>();
            Vector3 worldMin = boundsComp.Bounds.Min + transform.Position;
            Vector3 worldMax = boundsComp.Bounds.Max + transform.Position;
            var worldBounds = new BoundingBox(worldMin, worldMax);
            var boundsVeldrid = worldBounds.ToVeldridSpace();

            if (!frustum.Intersects(boundsVeldrid))
                return; // Skip culled entities
        }

        // Get or create instance group
        var key = (renderer.MeshId, renderer.Material);
        if (!instanceGroups.TryGetValue(key, out var group))
        {
            group = new InstanceGroup
            {
                MeshId = renderer.MeshId,
                Material = renderer.Material,
                Transforms = new List<Matrix4x4>(),
                GroupBounds = null
            };
            instanceGroups[key] = group;
        }

        // Add transform to instance group (in Veldrid space)
        group.Transforms.Add(transform.GetVeldridMatrix());
    }

    /// <summary>
    /// Process a renderer list for instancing (multiple instances per entity)
    /// </summary>
    private void ProcessRendererList(Entity entity, ref Transform transform, ref MeshRendererListComponent listRenderer,
        Camera camera, Frustum frustum, Dictionary<(Guid, Material), InstanceGroup> instanceGroups)
    {
        // Skip disabled renderers
        if (!listRenderer.Enabled)
            return;

        // Only process materials that support instancing
        if (!listRenderer.Material.SupportsInstancing)
            return;

        // Skip if no instances
        if (listRenderer.InstanceTransforms == null || listRenderer.InstanceTransforms.Length == 0)
            return;

        // Frustum culling at entity level
        if (entity.Has<BoundingBoxComponent>())
        {
            var boundsComp = entity.Get<BoundingBoxComponent>();
            Vector3 worldMin = boundsComp.Bounds.Min + transform.Position;
            Vector3 worldMax = boundsComp.Bounds.Max + transform.Position;
            var worldBounds = new BoundingBox(worldMin, worldMax);
            var boundsVeldrid = worldBounds.ToVeldridSpace();

            if (!frustum.Intersects(boundsVeldrid))
                return; // Skip all instances for this entity

            // LOD: Skip rendering if entity has TrackLODComponent and is beyond fade distance
            if (entity.Has<TrackLODComponent>())
            {
                float distance = Vector3.Distance(camera.Position, transform.Position);
                if (distance > TrackLODComponent.FadeStartDistance)
                {
                    return; // Skip instances - use LOD mesh instead
                }
            }
        }

        // Get or create instance group
        var key = (listRenderer.MeshId, listRenderer.Material);
        if (!instanceGroups.TryGetValue(key, out var group))
        {
            group = new InstanceGroup
            {
                MeshId = listRenderer.MeshId,
                Material = listRenderer.Material,
                Transforms = new List<Matrix4x4>(),
                GroupBounds = null
            };
            instanceGroups[key] = group;
        }

        // Get parent transform matrix (converts from entity's local space to world space, then to Veldrid space)
        Matrix4x4 parentVeldridMatrix = transform.GetVeldridMatrix();

        // Add all instance transforms to the group
        foreach (var instanceTransform in listRenderer.InstanceTransforms)
        {
            // Combine local instance transform with parent's world transform
            Matrix4x4 worldTransform = instanceTransform * parentVeldridMatrix;
            group.Transforms.Add(worldTransform);
        }
    }

    /// <summary>
    /// Create pipeline for an instanced material
    /// </summary>
    private PipelineResources CreatePipeline(Material material)
    {
        // Load shaders
        string vertexCode = LoadEmbeddedShader(material.VertexShader);
        string fragmentCode = LoadEmbeddedShader(material.FragmentShader);

        ShaderDescription vertexShaderDesc = new ShaderDescription(
            ShaderStages.Vertex,
            System.Text.Encoding.UTF8.GetBytes(vertexCode),
            "main");
        ShaderDescription fragmentShaderDesc = new ShaderDescription(
            ShaderStages.Fragment,
            System.Text.Encoding.UTF8.GetBytes(fragmentCode),
            "main");

        var shaders = _device.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

        // Create resource layout for uniforms
        var uniformLayout = _device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("InstanceUniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

        // Define vertex layout (per-vertex data)
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

        // Define instance layout (per-instance data: 4x4 matrix as 4 vec4s)
        var instanceLayout = new VertexLayoutDescription(
            new VertexElementDescription("InstanceTransform0", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("InstanceTransform1", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("InstanceTransform2", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("InstanceTransform3", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        instanceLayout.InstanceStepRate = 1;

        // Create pipeline
        GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = material.BlendState,
            DepthStencilState = material.DepthStencilState,
            RasterizerState = material.RasterizerState,
            PrimitiveTopology = material.PrimitiveTopology,
            ResourceLayouts = new[] { uniformLayout },
            ShaderSet = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout, instanceLayout },
                shaders: shaders),
            Outputs = _outputDescription
        };

        var pipeline = _device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

        // Create resource set
        var resourceSet = _device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            uniformLayout,
            _uniformBuffer));

        // Cleanup shaders
        foreach (var shader in shaders)
            shader.Dispose();

        _logger.Information("Created instanced pipeline for {VertexShader}/{FragmentShader}", material.VertexShader, material.FragmentShader);

        return new PipelineResources
        {
            Pipeline = pipeline,
            UniformLayout = uniformLayout,
            ResourceSet = resourceSet
        };
    }

    private static string LoadEmbeddedShader(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullResourceName = $"AlinasRailTools.Editor.Shaders.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded shader resource not found: {fullResourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public void Dispose()
    {
        _uniformBuffer?.Dispose();

        foreach (var res in _pipelineCache.Values)
        {
            res.Pipeline?.Dispose();
            res.UniformLayout?.Dispose();
            res.ResourceSet?.Dispose();
        }
        _pipelineCache.Clear();

        _instanceBuffer?.Dispose();

        _logger.Information("Disposed");
    }
}
