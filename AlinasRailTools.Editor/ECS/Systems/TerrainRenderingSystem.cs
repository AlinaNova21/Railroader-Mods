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
/// System that renders terrain tiles with per-tile heightmap textures
/// Each tile drawn separately due to unique heightmap texture binding
/// </summary>
public class TerrainRenderingSystem : ITerrainRenderingSystem
{
    private readonly ILogger _logger;
    private readonly GraphicsDevice _device;
    private readonly VeldridMesh _terrainMesh;
    private Pipeline _pipeline = null!;
    private ResourceLayout _uniformLayout = null!;
    private ResourceLayout _textureLayout = null!;
    private readonly Sampler _sampler;
    private readonly Dictionary<Entity, DeviceBuffer> _uniformBuffers = new();
    private readonly Dictionary<Entity, ResourceSet> _uniformResourceSets = new();

    [StructLayout(LayoutKind.Sequential)]
    private struct TerrainUniforms
    {
        public Matrix4x4 MVP;
        public Matrix4x4 Model;
        public Vector3 LightDir;
        private float _padding1;
        public Vector3 LightColor;
        private float _padding2;
        public Vector3 AmbientColor;
        private float _padding3;
        public Vector4 DiffuseColor;
        public Vector3 CameraPosition;
        public float FadeStart;
        public float FadeEnd;
        private float _padding4;
        private float _padding5;
        private float _padding6;
    }

    public TerrainRenderingSystem(GraphicsDevice device, OutputDescription outputDescription)
    {
        _logger = Log.ForContext<TerrainRenderingSystem>();
        _device = device;

        // Create shared terrain mesh (513x513 vertices, 100x100 subdivisions)
        _terrainMesh = VeldridMesh.CreatePlane(device, VeldridTerrainManager.TileDimension, VeldridTerrainManager.TileDimension, 100, 100);

        // Create sampler for heightmap textures
        _sampler = device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinLinear_MagLinear_MipLinear
        });

        // Create pipeline
        CreatePipeline(outputDescription);

        _logger.Information("Initialized");
    }

    private void CreatePipeline(OutputDescription outputDescription)
    {
        // Load terrain shaders (reuse existing shaders from VeldridTerrainManager)
        string vertexCode = LoadEmbeddedShader("terrain.vs");
        string fragmentCode = LoadEmbeddedShader("terrain.fs");

        ShaderDescription vertexShaderDesc = new ShaderDescription(
            ShaderStages.Vertex,
            System.Text.Encoding.UTF8.GetBytes(vertexCode),
            "main");
        ShaderDescription fragmentShaderDesc = new ShaderDescription(
            ShaderStages.Fragment,
            System.Text.Encoding.UTF8.GetBytes(fragmentCode),
            "main");

        var shaders = _device.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

        // Create resource layouts
        // Set 0: Uniform buffer (per tile uniforms)
        _uniformLayout = _device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("TerrainUniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

        // Set 1: Heightmap texture + sampler (per tile)
        _textureLayout = _device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("HeightmapTexture", ResourceKind.TextureReadOnly, ShaderStages.Vertex | ShaderStages.Fragment),
            new ResourceLayoutElementDescription("HeightmapSampler", ResourceKind.Sampler, ShaderStages.Vertex | ShaderStages.Fragment)));

        // Define vertex layout
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

        // Create pipeline
        GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleOverrideBlend,
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
            ResourceLayouts = new[] { _uniformLayout, _textureLayout },
            ShaderSet = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout },
                shaders: shaders),
            Outputs = outputDescription
        };

        _pipeline = _device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

        // Cleanup shaders
        foreach (var shader in shaders)
            shader.Dispose();

        _logger.Information("Created terrain pipeline");
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
    /// Render all loaded terrain tiles with frustum culling
    /// </summary>
    public void Render(World world, CommandList cl, Camera camera)
    {
        Matrix4x4 view = camera.GetViewMatrix();
        Matrix4x4 projection = camera.GetProjectionMatrix();
        Frustum frustum = camera.GetFrustum();

        // Set common rendering state
        cl.SetPipeline(_pipeline);
        cl.SetVertexBuffer(0, _terrainMesh.VertexBuffer);
        if (_terrainMesh.HasIndices)
        {
            cl.SetIndexBuffer(_terrainMesh.IndexBuffer!, IndexFormat.UInt32);
        }

        // Query all terrain tiles
        var query = new QueryDescription()
            .WithAll<TerrainTileComponent>();

        int totalTiles = 0;
        int loadedTiles = 0;
        int visibleTiles = 0;

        world.Query(in query, (Entity entity, ref TerrainTileComponent tile) =>
        {
            totalTiles++;

            // Skip tiles that don't have CPU data and GPU textures loaded yet
            if (!tile.IsLoaded || tile.HeightTexture == null || tile.HeightTextureView == null)
                return;

            // Safety check: Skip if texture is disposed (can happen during fast scrolling)
            try
            {
                if (tile.HeightTexture.IsDisposed || tile.HeightTextureView.IsDisposed)
                    return;
            }
            catch
            {
                // If accessing IsDisposed throws, the object is already gone
                return;
            }

            loadedTiles++;

            // Calculate tile world position (Unity space)
            float tileX = tile.TileCoord.X * VeldridTerrainManager.TileDimension;
            float tileZ = tile.TileCoord.Y * VeldridTerrainManager.TileDimension;
            float tileSize = VeldridTerrainManager.TileDimension;

            // Create bounding box for tile (Unity space)
            // Add padding to prevent edge popin at frustum boundaries
            float padding = 50.0f; // 50m padding on all sides
            BoundingBox tileBounds = new BoundingBox(
                new Vector3(tileX - padding, -100, tileZ - padding),
                new Vector3(tileX + tileSize + padding, 1100, tileZ + tileSize + padding)
            );

            // Convert to Veldrid space and test frustum
            var tileBoundsVeldrid = tileBounds.ToVeldridSpace();
            if (!frustum.Intersects(tileBoundsVeldrid))
            {
                return;
            }

            visibleTiles++;

            // Calculate model matrix (tile centered at origin of shared mesh, translated to world position)
            // Note: Z is negated because Unity +Z is Veldrid -Z
            Vector3 tileWorldPos = new Vector3(
                tileX + tileSize / 2,
                0,
                -(tileZ + tileSize / 2)
            );
            Matrix4x4 model = Matrix4x4.CreateTranslation(tileWorldPos);
            Matrix4x4 mvp = model * view * projection;

            // Prepare uniforms
            TerrainUniforms uniforms = new TerrainUniforms
            {
                MVP = mvp,
                Model = model,
                LightDir = Vector3.Normalize(new Vector3(0.5f, -1.0f, 0.3f)),
                LightColor = new Vector3(1.0f, 1.0f, 1.0f),
                AmbientColor = new Vector3(0.3f, 0.3f, 0.3f),
                DiffuseColor = new Vector4(0.6f, 0.4f, 0.2f, 1.0f),
                CameraPosition = camera.Target, // Use camera target (focal point) for fog fade, not camera position
                FadeStart = 18000.0f,  // 16 tiles * 500m
                FadeEnd = 19500.0f     // 19 tiles * 500m
            };

            // Get or create uniform buffer for this entity
            if (!_uniformBuffers.TryGetValue(entity, out var uniformBuffer))
            {
                uniformBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(
                    (uint)Marshal.SizeOf<TerrainUniforms>(),
                    BufferUsage.UniformBuffer | BufferUsage.Dynamic));
                _uniformBuffers[entity] = uniformBuffer;

                var uniformResourceSet = _device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    _uniformLayout,
                    uniformBuffer));
                _uniformResourceSets[entity] = uniformResourceSet;
            }

            // Update uniform buffer
            cl.UpdateBuffer(_uniformBuffers[entity], 0, uniforms);

            // Create texture resource set if needed (one-time per tile, cached in component)
            if (tile.HeightTextureResourceSet == null)
            {
                tile.HeightTextureResourceSet = _device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    _textureLayout,
                    tile.HeightTextureView,
                    _sampler));
            }

            // Bind resources
            cl.SetGraphicsResourceSet(0, _uniformResourceSets[entity]);
            cl.SetGraphicsResourceSet(1, tile.HeightTextureResourceSet);

            // Draw tile
            if (_terrainMesh.HasIndices)
            {
                cl.DrawIndexed(_terrainMesh.IndexCount);
            }
            else
            {
                cl.Draw(_terrainMesh.VertexCount);
            }
        });
    }

    public void Dispose()
    {
        _terrainMesh?.Dispose();
        _sampler?.Dispose();

        foreach (var uniformBuffer in _uniformBuffers.Values)
        {
            uniformBuffer?.Dispose();
        }
        _uniformBuffers.Clear();

        foreach (var resourceSet in _uniformResourceSets.Values)
        {
            resourceSet?.Dispose();
        }
        _uniformResourceSets.Clear();

        _uniformLayout?.Dispose();
        _textureLayout?.Dispose();
        _pipeline?.Dispose();

        _logger.Information("Disposed");
    }
}
