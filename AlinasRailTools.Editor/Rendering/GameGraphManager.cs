using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Extensions;
using Veldrid;
using Veldrid.SPIRV;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Serialization;
using AlinasRailTools.Editor.ECS.Components;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Manages loading and rendering of the game graph (track nodes as triangular arrows)
/// </summary>
public class GameGraphManager : IDisposable
{
    private readonly GraphicsDevice _device;
    private SerializedGameGraph? _gameGraph;
    private VeldridMesh? _arrowMesh;
    private VeldridMesh? _segmentsMesh;
    private Pipeline? _pipeline;
    private Shader[]? _shaders;
    private ResourceLayout? _uniformLayout;
    private DeviceBuffer? _uniformBuffer;
    private ResourceSet? _uniformResourceSet;

    [StructLayout(LayoutKind.Sequential)]
    private struct NodeUniforms
    {
        public Matrix4x4 MVP;
        public Vector4 Color;
    }

    private World? _world;

    // Lookup table: node ID -> entity
    private Dictionary<string, Entity> _nodeIdToEntity = new();

    public GameGraphManager(GraphicsDevice device)
    {
        _device = device;
    }

    /// <summary>
    /// Get the pipeline, uniform layout, and arrow mesh for rendering
    /// </summary>
    public (Pipeline pipeline, ResourceLayout uniformLayout, VeldridMesh arrowMesh) GetRenderingResources()
    {
        return (_pipeline!, _uniformLayout!, _arrowMesh!);
    }

    /// <summary>
    /// Load the game graph from file
    /// </summary>
    public void LoadGameGraph(string gameRootPath)
    {
        string graphPath = Path.Combine(gameRootPath, "game-graph-dump.json");
        if (!File.Exists(graphPath))
        {
            Console.WriteLine($"[GameGraphManager] game-graph-dump.json not found at: {graphPath}");
            return;
        }

        var serializer = new GameGraphSerializer();
        _gameGraph = serializer.LoadGameGraph(graphPath);

        int nodeCount = _gameGraph?.Tracks?.Nodes?.Count ?? 0;
        Console.WriteLine($"[GameGraphManager] Loaded game graph with {nodeCount} nodes");
    }

    /// <summary>
    /// Initialize only the rendering resources (shaders, pipeline, arrow mesh)
    /// Does not spawn entities - use with async loading + DatabaseSyncSystem
    /// </summary>
    public void InitializeRenderingOnly(OutputDescription? outputDescription = null)
    {
        // Create triangular arrow mesh
        _arrowMesh = CreateArrowMesh();
        Console.WriteLine($"[GameGraphManager] Created arrow mesh with {_arrowMesh.VertexCount} vertices");

        // Load shaders
        CreateShaders();
        CreatePipeline(outputDescription);

        Console.WriteLine("[GameGraphManager] Rendering resources initialized");
    }

    /// <summary>
    /// Initialize rendering resources and spawn entities (old synchronous method)
    /// </summary>
    public void Initialize(World world, OutputDescription? outputDescription = null)
    {
        _world = world;

        // Create triangular arrow mesh
        _arrowMesh = CreateArrowMesh();
        Console.WriteLine($"[GameGraphManager] Created arrow mesh with {_arrowMesh.VertexCount} vertices");

        // Create segments mesh from bezier curves
        CreateSegmentsMesh();

        // Load shaders
        CreateShaders();
        CreatePipeline(outputDescription);

        // Spawn entities for track nodes
        SpawnTrackNodeEntities();

        // Spawn entities for track segments
        SpawnTrackSegmentEntities();

        Console.WriteLine("[GameGraphManager] Initialized");
    }

    /// <summary>
    /// Get all tiles needed by the game graph
    /// </summary>
    public HashSet<Vector2Int> GetRequiredTiles()
    {
        HashSet<Vector2Int> tiles = new();

        if (_gameGraph?.Tracks?.Nodes == null)
            return tiles;

        foreach (var node in _gameGraph.Tracks.Nodes.Values)
        {
            if (node.Position == null)
                continue;

            // Calculate tile coordinate (Unity Z flips to rendering -Z)
            int tileX = (int)Math.Floor(node.Position.X / VeldridTerrainManager.TileDimension);
            int tileZ = (int)Math.Floor(node.Position.Z / VeldridTerrainManager.TileDimension); // Use Unity Z directly for tile calc
            tiles.Add(new Vector2Int(tileX, tileZ));
        }

        Console.WriteLine($"[GameGraphManager] Game graph requires {tiles.Count} unique tiles");

        // Debug: show sample node positions
        var sampleNode = _gameGraph.Tracks.Nodes.Values.FirstOrDefault();
        if (sampleNode?.Position != null)
        {
            Console.WriteLine($"[GameGraphManager] Sample node Unity position: ({sampleNode.Position.X:F1}, {sampleNode.Position.Y:F1}, {sampleNode.Position.Z:F1})");
            Console.WriteLine($"[GameGraphManager] Sample node rendering position: ({sampleNode.Position.X:F1}, {sampleNode.Position.Y:F1}, {-sampleNode.Position.Z:F1})");
        }

        return tiles;
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

    private void CreateShaders()
    {
        string vertexCode = LoadEmbeddedShader("arrow.vs");
        string fragmentCode = LoadEmbeddedShader("arrow.fs");

        ShaderDescription vertexShaderDesc = new ShaderDescription(
            ShaderStages.Vertex,
            System.Text.Encoding.UTF8.GetBytes(vertexCode),
            "main");
        ShaderDescription fragmentShaderDesc = new ShaderDescription(
            ShaderStages.Fragment,
            System.Text.Encoding.UTF8.GetBytes(fragmentCode),
            "main");

        _shaders = _device.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        Console.WriteLine("[GameGraphManager] Arrow shaders created");
    }

    private void CreatePipeline(OutputDescription? outputDescription = null)
    {
        // Create uniform buffer
        _uniformBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)Marshal.SizeOf<NodeUniforms>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        // Create resource layout
        _uniformLayout = _device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("NodeUniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

        // Create resource set
        _uniformResourceSet = _device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _uniformLayout,
            _uniformBuffer));

        // Create pipeline
        VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

        GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.None,  // No culling so arrows are visible from both sides
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.CounterClockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _uniformLayout },
            ShaderSet = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout },
                shaders: _shaders!),
            Outputs = outputDescription ?? _device.SwapchainFramebuffer.OutputDescription
        };

        _pipeline = _device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);
        Console.WriteLine("[GameGraphManager] Arrow pipeline created");
    }

    /// <summary>
    /// Create mesh for all track segments using bezier curves
    /// </summary>
    private void CreateSegmentsMesh()
    {
        if (_gameGraph?.Tracks?.Segments == null || _gameGraph.Tracks.Nodes == null)
        {
            Console.WriteLine("[GameGraphManager] No segments to render");
            return;
        }

        var vertices = new List<VeldridMesh.ColorVertex>();
        var indices = new List<uint>();

        float railGauge = 1.44f; // Standard gauge (1.435m)
        float railWidth = 0.15f; // Width of each rail (thicker for visibility)
        float railHeight = 0.05f; // Height of rail above ground
        int segmentsPerCurve = 20; // Number of subdivisions along each bezier curve
        Vector3 silverColor = new Vector3(0.75f, 0.75f, 0.75f); // Silver color for rails

        foreach (var segmentKvp in _gameGraph.Tracks.Segments)
        {
            var segment = segmentKvp.Value;

            // Look up start and end nodes
            if (!_gameGraph.Tracks.Nodes.TryGetValue(segment.StartId, out var startNode) ||
                !_gameGraph.Tracks.Nodes.TryGetValue(segment.EndId, out var endNode))
            {
                continue; // Skip segments with missing nodes
            }

            if (startNode.Position == null || startNode.Rotation == null ||
                endNode.Position == null || endNode.Rotation == null)
            {
                continue;
            }

            // Create bezier curve (matching game logic)
            var bezier = CreateBezierFromSegment(startNode, endNode);

            // Generate rail geometry along the curve (two separate rails)
            for (int i = 0; i <= segmentsPerCurve; i++)
            {
                float t = i / (float)segmentsPerCurve;
                Vector3 position = bezier.Evaluate(t);
                Vector3 tangent = Vector3.Normalize(bezier.GetTangent(t));

                // Always use horizontal plane for rails (ignore terrain banking)
                // Project tangent onto XZ plane and recalculate right vector
                Vector3 horizontalTangent = new Vector3(tangent.X, 0, tangent.Z);
                if (horizontalTangent.Length() < 0.001f)
                {
                    // Tangent is vertical, skip this segment
                    continue;
                }
                horizontalTangent = Vector3.Normalize(horizontalTangent);

                // Right vector is perpendicular to tangent in the horizontal plane
                Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, horizontalTangent));

                // Create vertices for left rail (horizontal rectangle at rail height)
                Vector3 leftRailCenter = position + right * (railGauge / 2) + new Vector3(0, railHeight, 0);
                uint leftRailStart = (uint)vertices.Count;
                vertices.Add(new VeldridMesh.ColorVertex(leftRailCenter - right * (railWidth / 2), silverColor));
                vertices.Add(new VeldridMesh.ColorVertex(leftRailCenter + right * (railWidth / 2), silverColor));

                // Create vertices for right rail (horizontal rectangle at rail height)
                Vector3 rightRailCenter = position - right * (railGauge / 2) + new Vector3(0, railHeight, 0);
                uint rightRailStart = (uint)vertices.Count;
                vertices.Add(new VeldridMesh.ColorVertex(rightRailCenter - right * (railWidth / 2), silverColor));
                vertices.Add(new VeldridMesh.ColorVertex(rightRailCenter + right * (railWidth / 2), silverColor));

                // Connect to previous segment (create separate quads for each rail)
                if (i > 0)
                {
                    // Each segment adds 4 vertices: left inner, left outer, right inner, right outer
                    // So previous segment is 4 vertices back

                    // Left rail quad
                    uint prevLeftInner = leftRailStart - 4;  // Previous segment's left inner
                    uint prevLeftOuter = leftRailStart - 3;  // Previous segment's left outer
                    uint currLeftInner = leftRailStart;
                    uint currLeftOuter = leftRailStart + 1;

                    indices.Add(prevLeftInner);
                    indices.Add(currLeftInner);
                    indices.Add(prevLeftOuter);

                    indices.Add(prevLeftOuter);
                    indices.Add(currLeftInner);
                    indices.Add(currLeftOuter);

                    // Right rail quad (fixed: was -2, -1 but should be -2, -1 from rightRailStart which is already +2 from leftRailStart)
                    uint prevRightInner = rightRailStart - 4;  // Go back 4 to previous segment's right inner
                    uint prevRightOuter = rightRailStart - 3;  // Go back 3 to previous segment's right outer
                    uint currRightInner = rightRailStart;
                    uint currRightOuter = rightRailStart + 1;

                    indices.Add(prevRightInner);
                    indices.Add(currRightInner);
                    indices.Add(prevRightOuter);

                    indices.Add(prevRightOuter);
                    indices.Add(currRightInner);
                    indices.Add(currRightOuter);
                }
            }
        }

        if (vertices.Count > 0)
        {
            _segmentsMesh = VeldridMesh.CreateColored(_device, vertices.ToArray(), indices.ToArray());
            Console.WriteLine($"[GameGraphManager] Created segments mesh: {vertices.Count} vertices, {indices.Count} indices");

            // Debug: show a sample position
            if (vertices.Count > 0)
            {
                var sample = vertices[0];
                Console.WriteLine($"[GameGraphManager] Sample segment vertex position: ({sample.Position.X:F1}, {sample.Position.Y:F1}, {sample.Position.Z:F1})");
            }
        }
        else
        {
            Console.WriteLine("[GameGraphManager] No segment vertices generated");
        }
    }

    /// <summary>
    /// Create bezier curve from track segment (matching game logic)
    /// Uses Unity coordinate space (+Z forward). RenderSystem handles conversion to Veldrid space.
    /// </summary>
    private BezierCurve CreateBezierFromSegment(SerializedTrackNode startNode, SerializedTrackNode endNode)
    {
        // Keep Unity coordinates (+Z forward)
        Vector3 startPos = new Vector3(startNode.Position!.X, startNode.Position.Y, startNode.Position.Z);
        Vector3 endPos = new Vector3(endNode.Position!.X, endNode.Position.Y, endNode.Position.Z);

        // Convert rotations to forward vectors (Y rotation in Unity is yaw)
        // Unity: +Z forward (standard right-hand coordinate system)
        float startYaw = startNode.Rotation!.Y * MathF.PI / 180.0f;
        float endYaw = endNode.Rotation!.Y * MathF.PI / 180.0f;

        Vector3 startForwardBase = new Vector3(MathF.Sin(startYaw), 0, MathF.Cos(startYaw));
        Vector3 endForwardBase = new Vector3(MathF.Sin(endYaw), 0, MathF.Cos(endYaw));

        // Determine which direction each node should point based on proximity
        // Use whichever end is closest to the other node
        Vector3 startToEnd = Vector3.Normalize(endPos - startPos);
        Vector3 endToStart = Vector3.Normalize(startPos - endPos);

        // If dot product is positive, use forward; if negative, use backward
        float startDot = Vector3.Dot(startForwardBase, startToEnd);
        float endDot = Vector3.Dot(endForwardBase, endToStart);

        Vector3 startForward = startDot >= 0 ? startForwardBase : -startForwardBase;
        Vector3 endForward = endDot >= 0 ? endForwardBase : -endForwardBase;

        // Debug: Log when we flip direction (this might indicate an issue)
        if (startDot < -0.5f || endDot < -0.5f)
        {
            Console.WriteLine($"[GameGraphManager] Bezier direction flip detected: start dot={startDot:F2}, end dot={endDot:F2}");
        }

        // Calculate tangent magnitude using game's formula
        float distance = (startPos - endPos).Length();
        float tangentFactor = BezierCurve.BezierTangentFactorForTangents(startForward, endForward);
        float tangentMagnitude = distance * tangentFactor;

        // Calculate control points
        Vector3 startTangent = startPos + startForward * tangentMagnitude;
        Vector3 endTangent = endPos - endForward * tangentMagnitude; // Negative because pointing backward from end

        // Up vectors (Unity Y-up)
        Vector3 up = Vector3.UnitY;

        return new BezierCurve(new[] { startPos, startTangent, endTangent, endPos }, up, up);
    }

    /// <summary>
    /// Create a triangular arrow mesh pointing in the +Z direction with rail lines
    /// </summary>
    private VeldridMesh CreateArrowMesh()
    {
        // Standard gauge is 1.435m (4ft 8.5in) between rails
        float railOffset = 0.72f;  // Distance from center to each rail (half of gauge)
        float railLength = 2.0f;   // Length of rails on arrow (much smaller to fit between track rails)
        float railWidth = 0.1f;    // Rail width (thicker for visibility)

        // Arrow should fit between the rails (narrower than gauge)
        float arrowLength = 2.0f;
        float arrowWidth = 0.5f;   // Half-width at base

        // Colors
        Vector3 redColor = new Vector3(1, 0, 0);       // Red arrow
        Vector3 silverColor = new Vector3(0.75f, 0.75f, 0.75f);  // Silver rails

        var vertices = new List<VeldridMesh.ColorVertex>
        {
            // Front-pointing triangle (in XZ plane at y=0.1 so it's above the rails)
            new VeldridMesh.ColorVertex(new Vector3(0, 0.1f, arrowLength), redColor),      // 0: Tip (front)
            new VeldridMesh.ColorVertex(new Vector3(-arrowWidth, 0.1f, 0), redColor),      // 1: Left base
            new VeldridMesh.ColorVertex(new Vector3(arrowWidth, 0.1f, 0), redColor),       // 2: Right base

            // Left rail (thin rectangle running along +Z)
            new VeldridMesh.ColorVertex(new Vector3(-railOffset - railWidth/2, 0, 0), silverColor),           // 3: Back left
            new VeldridMesh.ColorVertex(new Vector3(-railOffset + railWidth/2, 0, 0), silverColor),           // 4: Back right
            new VeldridMesh.ColorVertex(new Vector3(-railOffset - railWidth/2, 0, railLength), silverColor),  // 5: Front left
            new VeldridMesh.ColorVertex(new Vector3(-railOffset + railWidth/2, 0, railLength), silverColor),  // 6: Front right

            // Right rail (thin rectangle running along +Z)
            new VeldridMesh.ColorVertex(new Vector3(railOffset - railWidth/2, 0, 0), silverColor),            // 7: Back left
            new VeldridMesh.ColorVertex(new Vector3(railOffset + railWidth/2, 0, 0), silverColor),            // 8: Back right
            new VeldridMesh.ColorVertex(new Vector3(railOffset - railWidth/2, 0, railLength), silverColor),   // 9: Front left
            new VeldridMesh.ColorVertex(new Vector3(railOffset + railWidth/2, 0, railLength), silverColor)    // 10: Front right
        };

        var indices = new List<uint>
        {
            // Arrow triangle
            0, 1, 2,

            // Left rail (two triangles forming a thin rectangle)
            3, 5, 4,  // First triangle
            4, 5, 6,  // Second triangle

            // Right rail (two triangles forming a thin rectangle)
            7, 9, 8,  // First triangle
            8, 9, 10  // Second triangle
        };

        return VeldridMesh.CreateColored(_device, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Spawn entities for all track nodes
    /// </summary>
    private void SpawnTrackNodeEntities()
    {
        if (_world == null || _gameGraph?.Tracks?.Nodes == null || _arrowMesh == null || _pipeline == null || _uniformLayout == null)
        {
            Console.WriteLine("[GameGraphManager] Cannot spawn entities: missing required resources");
            return;
        }

        int spawnedCount = 0;

        foreach (var kvp in _gameGraph.Tracks.Nodes)
        {
            var node = kvp.Value;
            if (node.Position == null || node.Rotation == null)
                continue;

            // Keep Unity coordinates (+Z forward). RenderSystem handles conversion to Veldrid space.
            Vector3 position = new Vector3(node.Position.X, node.Position.Y, node.Position.Z);

            // Convert rotation from Euler angles (degrees) to quaternion
            // Unity uses Y-up, rotation.y is the yaw (rotation around Y axis)
            float yawRad = node.Rotation.Y * MathF.PI / 180.0f;
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(yawRad, 0, 0);

            // Create transform component
            var transform = new Transform(position, rotation, Vector3.One);

            // Create mesh component (shared arrow mesh)
            var meshComp = new MeshComponent(_arrowMesh);

            // Create material component (red arrows)
            var materialComp = new MaterialComponent(_pipeline, _uniformLayout, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));

            // Create track node component
            var trackNodeComp = new TrackNodeComponent(kvp.Key);

            // Spawn entity
            var entity = _world.Create(transform, meshComp, materialComp, new RenderableComponent(), trackNodeComp);

            // Add to lookup table
            _nodeIdToEntity[kvp.Key] = entity;

            spawnedCount++;
        }

        Console.WriteLine($"[GameGraphManager] Spawned {spawnedCount} track node entities");
    }

    /// <summary>
    /// Spawn entities for all track segments
    /// </summary>
    private void SpawnTrackSegmentEntities()
    {
        if (_world == null || _gameGraph?.Tracks?.Segments == null)
        {
            Console.WriteLine("[GameGraphManager] Cannot spawn segment entities: missing required resources");
            return;
        }

        int spawnedCount = 0;
        int skippedCount = 0;

        foreach (var kvp in _gameGraph.Tracks.Segments)
        {
            var segment = kvp.Value;

            // Look up entity references for start and end nodes
            if (!_nodeIdToEntity.TryGetValue(segment.StartId, out var startEntity) ||
                !_nodeIdToEntity.TryGetValue(segment.EndId, out var endEntity))
            {
                skippedCount++;
                continue;
            }

            // Create segment component
            var segmentComp = new TrackSegmentComponent(kvp.Key, startEntity, endEntity);

            // Create bezier curve component (starts empty, will be calculated by CurveUpdateSystem)
            var curveComp = new BezierCurveComponent();

            // Spawn segment entity
            _world.Create(segmentComp, curveComp);
            spawnedCount++;
        }

        Console.WriteLine($"[GameGraphManager] Spawned {spawnedCount} track segment entities ({skippedCount} skipped)");
    }

    /// <summary>
    /// Render track segments (bezier curves)
    /// Note: Track node arrows are now rendered via ECS RenderSystem
    /// </summary>
    public void Render(CommandList cl, Camera camera)
    {
        if (_gameGraph == null || _pipeline == null)
            return;

        cl.SetPipeline(_pipeline);

        Matrix4x4 view = camera.GetViewMatrix();
        Matrix4x4 projection = camera.GetProjectionMatrix();

        // Render track segments (bezier curves)
        if (_segmentsMesh != null)
        {
            cl.SetVertexBuffer(0, _segmentsMesh.VertexBuffer);
            if (_segmentsMesh.HasIndices)
            {
                cl.SetIndexBuffer(_segmentsMesh.IndexBuffer!, IndexFormat.UInt32);
            }

            // Segments are in Unity space, convert to Veldrid space (flip Z)
            Matrix4x4 unityToVeldrid = Matrix4x4.CreateScale(1, 1, -1);
            Matrix4x4 mvp = unityToVeldrid * view * projection;

            NodeUniforms uniforms = new NodeUniforms
            {
                MVP = mvp,
                Color = new Vector4(0.75f, 0.75f, 0.75f, 1.0f)  // Silver segments
            };

            cl.UpdateBuffer(_uniformBuffer!, 0, uniforms);
            cl.SetGraphicsResourceSet(0, _uniformResourceSet!);

            if (_segmentsMesh.HasIndices)
            {
                cl.DrawIndexed(_segmentsMesh.IndexCount);
            }
            else
            {
                cl.Draw(_segmentsMesh.VertexCount);
            }
        }
    }

    public void Dispose()
    {
        _arrowMesh?.Dispose();
        _segmentsMesh?.Dispose();
        _uniformResourceSet?.Dispose();
        _uniformBuffer?.Dispose();
        _uniformLayout?.Dispose();
        _pipeline?.Dispose();
        if (_shaders != null)
        {
            foreach (var shader in _shaders)
                shader.Dispose();
        }
    }
}
