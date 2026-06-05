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
/// System that renders railroad ties using true GPU instancing
/// Uses a single tie mesh and instance buffers for maximum performance
/// </summary>
public class TieRenderingSystem : ITieRenderingSystem
{
    private readonly ILogger _logger;
    private readonly GraphicsDevice _device;
    private readonly VeldridMesh _tieMesh;
    private Pipeline _pipeline = null!;
    private ResourceLayout _uniformLayout = null!;
    private readonly DeviceBuffer _uniformBuffer;
    private readonly ResourceSet _resourceSet;

    // Tie dimensions
    private readonly float _tieLength = 2.5f;
    private readonly float _tieWidth = 0.25f;
    private readonly float _tieHeight = 0.15f;
    private readonly Vector3 _tieColor = new Vector3(0.4f, 0.3f, 0.2f);

    [StructLayout(LayoutKind.Sequential)]
    private struct TieUniforms
    {
        public Matrix4x4 ViewProjection;
        public Vector4 Color;
    }

    public TieRenderingSystem(GraphicsDevice device, OutputDescription outputDescription)
    {
        _logger = Log.ForContext<TieRenderingSystem>();
        _device = device;

        // Create single tie mesh (will be instanced)
        _tieMesh = CreateTieMesh();

        // Create instanced shaders and pipeline
        CreateInstancedPipeline(outputDescription);

        // Create shared uniform buffer
        _uniformBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)Marshal.SizeOf<TieUniforms>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        // Create resource set
        _resourceSet = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _uniformLayout!,
            _uniformBuffer));

        _logger.Information("Initialized with GPU instancing");
    }

    private void CreateInstancedPipeline(OutputDescription outputDescription)
    {
        // Load instanced shaders (uses common color fragment shader)
        string vertexCode = LoadEmbeddedShader("tie_instanced.vs");
        string fragmentCode = LoadEmbeddedShader("common_color.fs");

        ShaderDescription vertexShaderDesc = new ShaderDescription(
            ShaderStages.Vertex,
            System.Text.Encoding.UTF8.GetBytes(vertexCode),
            "main");
        ShaderDescription fragmentShaderDesc = new ShaderDescription(
            ShaderStages.Fragment,
            System.Text.Encoding.UTF8.GetBytes(fragmentCode),
            "main");

        var shaders = _device.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

        // Create resource layout for view-projection uniform
        _uniformLayout = _device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("TieUniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

        // Define vertex layout (per-vertex data: position + color)
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

        // Define instance layout (per-instance data: 4x4 matrix as 4 vec4s)
        // Matrix4x4 from C# is row-major, shader will treat rows as columns (effective transpose)
        var instanceLayout = new VertexLayoutDescription(
            new VertexElementDescription("InstanceTransform0", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("InstanceTransform1", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("InstanceTransform2", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("InstanceTransform3", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        instanceLayout.InstanceStepRate = 1; // Advance per instance, not per vertex

        // Create pipeline
        GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.CounterClockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _uniformLayout },
            ShaderSet = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout, instanceLayout },
                shaders: shaders),
            Outputs = outputDescription
        };

        _pipeline = _device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

        // Cleanup shaders
        foreach (var shader in shaders)
            shader.Dispose();

        _logger.Information("Created instanced pipeline");
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

    /// <summary>
    /// Create a single tie mesh (rectangular box)
    /// </summary>
    private VeldridMesh CreateTieMesh()
    {
        var vertices = new List<VeldridMesh.ColorVertex>();
        var indices = new List<uint>();

        // 8 vertices for rectangular box (centered at origin)
        // Bottom face (4 vertices)
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(-_tieLength/2, -_tieHeight/2, -_tieWidth/2), _tieColor)); // 0
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(+_tieLength/2, -_tieHeight/2, -_tieWidth/2), _tieColor)); // 1
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(+_tieLength/2, -_tieHeight/2, +_tieWidth/2), _tieColor)); // 2
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(-_tieLength/2, -_tieHeight/2, +_tieWidth/2), _tieColor)); // 3

        // Top face (4 vertices)
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(-_tieLength/2, +_tieHeight/2, -_tieWidth/2), _tieColor)); // 4
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(+_tieLength/2, +_tieHeight/2, -_tieWidth/2), _tieColor)); // 5
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(+_tieLength/2, +_tieHeight/2, +_tieWidth/2), _tieColor)); // 6
        vertices.Add(new VeldridMesh.ColorVertex(new Vector3(-_tieLength/2, +_tieHeight/2, +_tieWidth/2), _tieColor)); // 7

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

    /// <summary>
    /// Render all ties using true GPU instancing with instance buffers
    /// </summary>
    public void Render(World world, CommandList cl, Camera camera)
    {
        Matrix4x4 view = camera.GetViewMatrix();
        Matrix4x4 projection = camera.GetProjectionMatrix();
        Matrix4x4 viewProjection = view * projection;
        Frustum frustum = camera.GetFrustum();

        // Update uniforms (view-projection only, model matrices come from instance buffer)
        TieUniforms uniforms = new TieUniforms
        {
            ViewProjection = viewProjection,
            Color = new Vector4(_tieColor.X, _tieColor.Y, _tieColor.Z, 1.0f)
        };
        cl.UpdateBuffer(_uniformBuffer, 0, uniforms);

        // Set common rendering state
        cl.SetPipeline(_pipeline);
        cl.SetGraphicsResourceSet(0, _resourceSet);
        cl.SetVertexBuffer(0, _tieMesh.VertexBuffer);
        if (_tieMesh.HasIndices)
        {
            cl.SetIndexBuffer(_tieMesh.IndexBuffer!, IndexFormat.UInt32);
        }

        // Query all entities with tie instances
        var query = new QueryDescription()
            .WithAll<TieInstanceComponent>();

        int totalSegments = 0;
        int renderedSegments = 0;
        int totalTies = 0;
        int renderedTies = 0;

        world.Query(in query, (Entity entity, ref TieInstanceComponent tieComp) =>
        {
            if (tieComp.InstanceTransforms == null || tieComp.InstanceTransforms.Length == 0)
                return;

            totalSegments++;
            totalTies += tieComp.InstanceTransforms.Length;

            // Frustum cull the entire segment if it has a bounding box
            if (entity.Has<BoundingBoxComponent>())
            {
                var boundsComp = entity.Get<BoundingBoxComponent>();

                // Get transform if available for world-space bounds calculation
                Transform? transform = entity.Has<Transform>() ? entity.Get<Transform>() : null;

                if (transform.HasValue)
                {
                    // Bounding box is in local space, transform to world space
                    Vector3 worldMin = boundsComp.Bounds.Min + transform.Value.Position;
                    Vector3 worldMax = boundsComp.Bounds.Max + transform.Value.Position;
                    var worldBounds = new BoundingBox(worldMin, worldMax);

                    // Convert bounds from Unity space to Veldrid space for frustum test
                    var boundsVeldrid = worldBounds.ToVeldridSpace();
                    if (!frustum.Intersects(boundsVeldrid))
                    {
                        return; // Skip all ties for this segment
                    }

                    // LOD: Skip rendering ties if segment is beyond fade start distance
                    if (entity.Has<TrackLODComponent>())
                    {
                        float distance = Vector3.Distance(camera.Position, transform.Value.Position);

                        // Don't render ties if we're in the fade range or beyond
                        if (distance > TrackLODComponent.FadeStartDistance)
                        {
                            return; // Skip ties for this segment - use LOD mesh instead
                        }
                    }
                }
            }

            renderedSegments++;
            renderedTies += tieComp.InstanceTransforms.Length;

            // Get parent transform to convert local ties to world space
            Matrix4x4 parentTransform = Matrix4x4.Identity;
            if (entity.Has<Transform>())
            {
                parentTransform = entity.Get<Transform>().GetVeldridMatrix();
            }

            // Transform each tie from local space to world space
            var worldTransforms = new Matrix4x4[tieComp.InstanceTransforms.Length];
            for (int i = 0; i < tieComp.InstanceTransforms.Length; i++)
            {
                // Combine local tie transform with parent's world transform
                worldTransforms[i] = tieComp.InstanceTransforms[i] * parentTransform;
            }

            // Create a new instance buffer for each segment to avoid data leakage
            uint requiredSize = (uint)(worldTransforms.Length * Marshal.SizeOf<Matrix4x4>());

            // Create a temporary instance buffer for this segment
            using var instanceBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(
                requiredSize,
                BufferUsage.VertexBuffer | BufferUsage.Dynamic));

            // Upload instance data to GPU (Matrix4x4 is row-major in C#, will be treated as columns in GLSL)
            cl.UpdateBuffer(instanceBuffer, 0, worldTransforms);

            // Set instance buffer
            cl.SetVertexBuffer(1, instanceBuffer);

            // Draw all instances in a single call!
            if (_tieMesh.HasIndices)
            {
                cl.DrawIndexed(
                    indexCount: _tieMesh.IndexCount,
                    instanceCount: (uint)tieComp.InstanceTransforms.Length,
                    indexStart: 0,
                    vertexOffset: 0,
                    instanceStart: 0);
            }
            else
            {
                cl.Draw(
                    vertexCount: _tieMesh.VertexCount,
                    instanceCount: (uint)tieComp.InstanceTransforms.Length,
                    vertexStart: 0,
                    instanceStart: 0);
            }
        });

        // Uncomment for debugging
        // if (totalSegments > 0)
        //     Console.WriteLine($"[TieRenderingSystem] Rendered {renderedTies}/{totalTies} ties in {renderedSegments}/{totalSegments} segments");
    }

    public void Dispose()
    {
        _tieMesh?.Dispose();
        _uniformBuffer?.Dispose();
        _resourceSet?.Dispose();
        _uniformLayout?.Dispose();
        _pipeline?.Dispose();

        _logger.Information("Disposed");
    }
}
