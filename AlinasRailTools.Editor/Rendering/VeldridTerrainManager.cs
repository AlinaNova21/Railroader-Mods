using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.SPIRV;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AlinasRailTools.Editor.Rendering;

public class VeldridTerrainManager : IDisposable
{
    /// <summary>
    /// Tile dimension in meters (from Map.json)
    /// Each tile is 500x500 meters, represented by a 513x513 heightmap
    /// </summary>
    public const float TileDimension = 500.0f;

    private readonly GraphicsDevice _device;
    private Pipeline? _pipeline;
    private Shader[]? _shaders;
    private ResourceLayout? _uniformLayout; // Layout for uniform buffer (set 0)
    private ResourceLayout? _textureLayout; // Layout for texture + sampler (set 1)
    private Sampler? _sampler;
    private VeldridMesh? _sharedMesh; // Single mesh shared by all tiles
    private VeldridTexture? _flatHeightmap; // Shared flat heightmap (500m elevation)
    private ResourceSet? _textureResourceSet; // Single shared resource set (updated per tile)

    // Background plane
    private Pipeline? _backgroundPipeline;
    private Shader[]? _backgroundShaders;
    private ResourceLayout? _backgroundUniformLayout;
    private DeviceBuffer? _backgroundUniformBuffer;
    private ResourceSet? _backgroundResourceSet;
    private VeldridMesh? _backgroundMesh;

    // Track tile paths and pending loads
    private readonly Dictionary<Vector2Int, string?> _tilePathCache = new(); // Cache tile paths (null = not found)
    private readonly object _tilePathCacheLock = new object(); // Lock for cache access
    private readonly Queue<PendingTileLoad> _pendingLoads = new();
    private readonly object _pendingLoadsLock = new object();

    private string? _gameRootDir;
    public string MapName { get; set; } = "BushnellWhittier";
    public int TileLoadRadius { get; set; } = 20;  // Load 21x21 tiles around camera
    public int TileRenderRadius { get; set; } = 12;
    public float TileFadeStartRadius { get; set; } = 9.0f;  // Start fading height at 9 tiles
    public float TileFadeEndRadius { get; set; } = 10.0f;  // Fully flat by 10 tiles (1 tile transition, 2 tiles flat)

    public class PendingTileLoad
    {
        public Vector2Int Coordinate { get; set; }
        public byte[] HeightData { get; set; }
        public float[] HeightmapData { get; set; } // Pre-converted normalized heightmap (0-1 range)
        public int Width { get; set; }
        public int Height { get; set; }

        public PendingTileLoad(Vector2Int coordinate, byte[] heightData, float[] heightmapData, int width, int height)
        {
            Coordinate = coordinate;
            HeightData = heightData;
            HeightmapData = heightmapData;
            Width = width;
            Height = height;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BackgroundUniforms
    {
        public Matrix4x4 MVP;
    }

    public VeldridTerrainManager(GraphicsDevice device)
    {
        _device = device;
        _gameRootDir = FindGameRootDirectory();
        Console.WriteLine($"[VeldridTerrainManager] Game root: {_gameRootDir}");
    }

    public string GetGameRootDirectory() => _gameRootDir!;

    private string FindGameRootDirectory()
    {
        string currentDir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(currentDir))
        {
            string testPath = Path.Combine(currentDir, "Railroader_Data");
            if (Directory.Exists(testPath))
                return currentDir;

            var parent = Directory.GetParent(currentDir);
            if (parent == null)
                break;
            currentDir = parent.FullName;
        }
        throw new DirectoryNotFoundException("Could not find Railroader game directory");
    }

    public void Initialize(OutputDescription? outputDescription = null)
    {
        Console.WriteLine("[VeldridTerrainManager] Initializing terrain system");

        // Create shared mesh (one for all tiles)
        _sharedMesh = VeldridMesh.CreatePlane(_device, TileDimension, TileDimension, 100, 100);
        Console.WriteLine("[VeldridTerrainManager] Shared terrain mesh created: 10201 vertices, 60000 indices");

        // Create shaders and pipeline once
        CreateShaders();
        CreatePipeline(outputDescription);

        // Create flat heightmap (all zeros = 500m with our formula)
        CreateFlatHeightmap();

        // Create background plane
        CreateBackgroundPlane(outputDescription);

        Console.WriteLine("[VeldridTerrainManager] Terrain system initialized");
    }

    /// <summary>
    /// Asynchronously discover all available tiles - scan directories and cache paths in background
    /// </summary>
    public void PrelocateTilesAsync()
    {
        Console.WriteLine($"[VeldridTerrainManager] Starting async tile discovery...");
        Task.Run(async () =>
        {
            try
            {
                await PrelocateTilesAsyncInternal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VeldridTerrainManager] Error during tile discovery: {ex.Message}");
            }
        });
    }

    private async Task PrelocateTilesAsyncInternal()
    {
        int baseGameTiles = 0;
        int modTiles = 0;

        // 1. Scan base game tiles first (these take priority)
        string baseGameMapsPath = Path.Combine(_gameRootDir!, "Railroader_Data", "StreamingAssets", "Maps", MapName);
        if (Directory.Exists(baseGameMapsPath))
        {
            foreach (string tilePath in Directory.GetFiles(baseGameMapsPath, "tile_*.data"))
            {
                var coord = ParseTileCoordinate(Path.GetFileNameWithoutExtension(tilePath));
                if (coord.HasValue)
                {
                    lock (_tilePathCacheLock)
                    {
                        _tilePathCache[coord.Value] = tilePath;
                    }
                    baseGameTiles++;
                }

                // Yield occasionally to keep responsive
                if (baseGameTiles % 100 == 0)
                {
                    await Task.Yield();
                }
            }
        }

        // 2. Scan mod directories
        List<string> modSearchPaths = new()
        {
            Path.Combine(_gameRootDir!, "Mods"),
            Path.Combine(_gameRootDir!, "Railroader_Data", "Mods")
        };

        foreach (string modsPath in modSearchPaths)
        {
            if (!Directory.Exists(modsPath))
                continue;

            foreach (string modDir in Directory.GetDirectories(modsPath))
            {
                string modMapsPath = Path.Combine(modDir, "Maps", MapName);
                if (!Directory.Exists(modMapsPath))
                    continue;

                string modName = Path.GetFileName(modDir);
                int modTileCount = 0;

                foreach (string tilePath in Directory.GetFiles(modMapsPath, "tile_*.data"))
                {
                    var coord = ParseTileCoordinate(Path.GetFileNameWithoutExtension(tilePath));
                    if (coord.HasValue)
                    {
                        lock (_tilePathCacheLock)
                        {
                            if (!_tilePathCache.ContainsKey(coord.Value))
                            {
                                _tilePathCache[coord.Value] = tilePath;
                                modTileCount++;
                                modTiles++;
                            }
                        }
                    }

                    // Yield occasionally
                    if (modTiles % 100 == 0)
                    {
                        await Task.Yield();
                    }
                }

                if (modTileCount > 0)
                {
                    Console.WriteLine($"[VeldridTerrainManager] Mod '{modName}': {modTileCount} tiles discovered");
                }
            }
        }

        Console.WriteLine($"[VeldridTerrainManager] Tile discovery complete: {baseGameTiles} base game, {modTiles} from mods (total: {baseGameTiles + modTiles})");
    }

    /// <summary>
    /// Prelocate all tiles - scan all directories and cache paths for all available tiles
    /// </summary>
    public void PrelocateTiles(HashSet<Vector2Int> requiredTiles)
    {
        Console.WriteLine($"[VeldridTerrainManager] Prelocating all available tiles...");
        Console.WriteLine($"[VeldridTerrainManager] Game root: {_gameRootDir}");
        Console.WriteLine($"[VeldridTerrainManager] Map name: {MapName}");

        // Scan and cache ALL available tiles from all sources
        int baseGameTiles = 0;
        int modTiles = 0;
        int skippedConflicts = 0;

        // 1. Scan base game tiles first (these take priority)
        string baseGameMapsPath = Path.Combine(_gameRootDir!, "Railroader_Data", "StreamingAssets", "Maps", MapName);
        if (Directory.Exists(baseGameMapsPath))
        {
            foreach (string tilePath in Directory.GetFiles(baseGameMapsPath, "tile_*.data"))
            {
                var coord = ParseTileCoordinate(Path.GetFileNameWithoutExtension(tilePath));
                if (coord.HasValue)
                {
                    _tilePathCache[coord.Value] = tilePath;
                    baseGameTiles++;
                }
            }
        }

        // 2. Scan mod directories (skip if already in cache from base game)
        List<string> modSearchPaths = new()
        {
            Path.Combine(_gameRootDir!, "Mods"),
            Path.Combine(_gameRootDir!, "Railroader_Data", "Mods")
        };

        foreach (string modsPath in modSearchPaths)
        {
            if (!Directory.Exists(modsPath))
                continue;

            foreach (string modDir in Directory.GetDirectories(modsPath))
            {
                string modMapsPath = Path.Combine(modDir, "Maps", MapName);
                if (!Directory.Exists(modMapsPath))
                    continue;

                string modName = Path.GetFileName(modDir);
                int modTileCount = 0;
                int modConflicts = 0;

                foreach (string tilePath in Directory.GetFiles(modMapsPath, "tile_*.data"))
                {
                    var coord = ParseTileCoordinate(Path.GetFileNameWithoutExtension(tilePath));
                    if (coord.HasValue)
                    {
                        if (_tilePathCache.ContainsKey(coord.Value))
                        {
                            // Already have this tile (from base game or earlier mod), skip
                            modConflicts++;
                            skippedConflicts++;
                        }
                        else
                        {
                            _tilePathCache[coord.Value] = tilePath;
                            modTileCount++;
                            modTiles++;
                        }
                    }
                }

                if (modTileCount > 0 || modConflicts > 0)
                {
                    Console.WriteLine($"[VeldridTerrainManager] Mod '{modName}': {modTileCount} tiles cached, {modConflicts} conflicts skipped");
                }
            }
        }

        Console.WriteLine($"[VeldridTerrainManager] Total tiles cached: {baseGameTiles} base game, {modTiles} from mods, {skippedConflicts} conflicts skipped");

        // Check which required tiles are missing
        List<string> missingTiles = new();
        foreach (var tile in requiredTiles)
        {
            if (!_tilePathCache.TryGetValue(tile, out string? tilePath) || tilePath == null)
            {
                missingTiles.Add($"tile_{FormatCoordinate(tile.X)}_{FormatCoordinate(tile.Y)}.data");
            }
        }

        // Fail early if any required tiles are missing
        if (missingTiles.Count > 0)
        {
            Console.WriteLine($"[VeldridTerrainManager] ERROR: {missingTiles.Count} required tiles are missing:");
            foreach (var missing in missingTiles.Take(10))
            {
                Console.WriteLine($"  - {missing}");
            }
            if (missingTiles.Count > 10)
            {
                Console.WriteLine($"  ... and {missingTiles.Count - 10} more");
            }
            throw new FileNotFoundException($"Missing {missingTiles.Count} required terrain tiles");
        }

        Console.WriteLine($"[VeldridTerrainManager] Prelocated successfully - all {requiredTiles.Count} required tiles found");
    }

    /// <summary>
    /// Parse tile coordinate from filename like "tile_001_002" -> (1, 2) or "tile_-003_004" -> (-3, 4)
    /// </summary>
    private Vector2Int? ParseTileCoordinate(string fileName)
    {
        // Expected format: tile_XXX_YYY or tile_-XXX_YYY
        if (!fileName.StartsWith("tile_"))
            return null;

        string coords = fileName.Substring(5); // Remove "tile_" prefix
        string[] parts = coords.Split('_');
        if (parts.Length != 2)
            return null;

        // Parse coordinates (handle negative numbers)
        if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int z))
        {
            return new Vector2Int(x, z);
        }

        return null;
    }

    private void CreateFlatHeightmap()
    {
        // Create a full-size flat heightmap (513x513, all zeros = 500m elevation)
        // This matches the real terrain tile size to avoid sampling artifacts
        int size = 513;
        byte[] flatData = new byte[size * size * 2]; // R16 texture (2 bytes per pixel)
        // All zeros - R16_UNorm value of 0.0 maps to 500m with our formula: (0.0 * 1000) + 500 = 500

        _flatHeightmap = VeldridTexture.CreateR16FromBytes(_device, size, size, flatData);

        // Create single shared texture resource set (set 1) - will be updated per tile
        _textureResourceSet = _device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _textureLayout!,
            _flatHeightmap.TextureView,
            _sampler!));

        Console.WriteLine("[VeldridTerrainManager] Flat placeholder heightmap and shared resource set created (513x513)");
    }

    private void CreateBackgroundPlane(OutputDescription? outputDescription = null)
    {
        // Create a large background mesh (covers about 50x50 tiles = 25600x25600 units)
        // This is centered at origin and sits at y=500
        float size = 25600.0f;
        _backgroundMesh = VeldridMesh.CreatePlane(_device, size, size, 2, 2); // Very simple mesh, just a quad
        Console.WriteLine($"[VeldridTerrainManager] Background plane mesh created: {size}x{size}");

        // Load background shaders
        string vertexCode = LoadEmbeddedShader("background.vs");
        string fragmentCode = LoadEmbeddedShader("background.fs");

        ShaderDescription vertexShaderDesc = new ShaderDescription(
            ShaderStages.Vertex,
            System.Text.Encoding.UTF8.GetBytes(vertexCode),
            "main");
        ShaderDescription fragmentShaderDesc = new ShaderDescription(
            ShaderStages.Fragment,
            System.Text.Encoding.UTF8.GetBytes(fragmentCode),
            "main");

        _backgroundShaders = _device.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        Console.WriteLine("[VeldridTerrainManager] Background shaders created");

        // Create background uniform buffer
        _backgroundUniformBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)Marshal.SizeOf<BackgroundUniforms>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        // Create background resource layout
        _backgroundUniformLayout = _device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("BackgroundUniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

        // Create background resource set
        _backgroundResourceSet = _device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _backgroundUniformLayout,
            _backgroundUniformBuffer));

        // Create background pipeline - when using SPIRV, ALL elements use TextureCoordinate semantic
        // DO NOT specify offsets or stride - let Veldrid calculate them
        VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),   // location 0
            new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),   // location 1
            new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));    // location 2

        GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleDisabled, // No blending for background
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
            ResourceLayouts = new[] { _backgroundUniformLayout },
            ShaderSet = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout },
                shaders: _backgroundShaders!),
            Outputs = outputDescription ?? _device.SwapchainFramebuffer.OutputDescription
        };

        _backgroundPipeline = _device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);
        Console.WriteLine("[VeldridTerrainManager] Background pipeline created");
    }

    /// <summary>
    /// Public method to load a tile asynchronously and invoke a callback when done
    /// Used by ECS terrain system
    /// </summary>
    public void LoadTileAsync(int tileX, int tileZ, Action<int, int, bool> onComplete)
    {
        Vector2Int tileCoord = new Vector2Int(tileX, tileZ);

        // Check if tile path is in cache (thread-safe)
        string? tilePath;
        lock (_tilePathCacheLock)
        {
            if (!_tilePathCache.TryGetValue(tileCoord, out tilePath))
            {
                // Tile not discovered yet - invoke callback with failure
                onComplete?.Invoke(tileX, tileZ, false);
                return;
            }
        }

        if (tilePath == null)
        {
            // Tile was discovered but doesn't exist
            onComplete?.Invoke(tileX, tileZ, false);
            return;
        }

        // Start async load on background thread
        string pathCopy = tilePath; // Capture for closure
        Task.Run(() => LoadHeightmapDataAsync(tileCoord, pathCopy, onComplete));
    }

    private void LoadHeightmapDataAsync(Vector2Int tileCoord, string path, Action<int, int, bool>? callback = null)
    {
        try
        {
            // Load and decode image on background thread
            using var stream = File.OpenRead(path);
            using var image = Image.Load<Rgba32>(stream);

            if (image.Width != 513 || image.Height != 513)
            {
                Console.WriteLine($"[VeldridTerrainManager] Invalid terrain tile dimensions: {image.Width}x{image.Height}");
                callback?.Invoke(tileCoord.X, tileCoord.Y, false);
                return;
            }

            // Extract RG bytes (R=high byte, G=low byte of height)
            // AND convert to normalized float array (do heavy lifting on background thread)
            int pixelCount = image.Width * image.Height;
            byte[] heightData = new byte[pixelCount * 2];
            float[] heightmapData = new float[pixelCount];

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    int idx = (y * image.Width + x) * 2;
                    int pixelIdx = y * image.Width + x;

                    // Little-endian: low byte first, then high byte
                    byte lowByte = pixel.G;
                    byte highByte = pixel.R;
                    heightData[idx] = lowByte;
                    heightData[idx + 1] = highByte;

                    // Convert to normalized 0-1 float (R16 format)
                    ushort rawValue = (ushort)(lowByte | (highByte << 8));
                    heightmapData[pixelIdx] = rawValue / 65535.0f;
                }
            }

            // Queue for GPU resource creation on main thread (data already converted!)
            lock (_pendingLoadsLock)
            {
                _pendingLoads.Enqueue(new PendingTileLoad(tileCoord, heightData, heightmapData, image.Width, image.Height));
            }

            // Invoke callback indicating success (texture will be created on main thread later)
            callback?.Invoke(tileCoord.X, tileCoord.Y, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VeldridTerrainManager] Failed to load tile {tileCoord}: {ex.Message}");
            callback?.Invoke(tileCoord.X, tileCoord.Y, false);
        }
    }

    /// <summary>
    /// Create GPU texture from heightmap data (public method for ECS system)
    /// </summary>
    public static VeldridTexture CreateHeightmapTexture(GraphicsDevice device, byte[] heightData, int width, int height)
    {
        return VeldridTexture.CreateR16FromBytes(device, width, height, heightData);
    }

    /// <summary>
    /// Try to dequeue a pending tile load (for ECS system to process)
    /// Returns null if no pending loads
    /// </summary>
    public PendingTileLoad? DequeuePendingLoad()
    {
        lock (_pendingLoadsLock)
        {
            if (_pendingLoads.Count > 0)
            {
                return _pendingLoads.Dequeue();
            }
        }
        return null;
    }

    private string FormatCoordinate(int coord)
    {
        return coord < 0 ? $"-{Math.Abs(coord):D3}" : $"{coord:D3}";
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
        // Load shaders from embedded resources
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

        _shaders = _device.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        Console.WriteLine("[VeldridTerrainManager] Shaders created");
    }

    private void CreatePipeline(OutputDescription? outputDescription = null)
    {

        // Create sampler
        _sampler = _device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinLinear_MagLinear_MipLinear
        });

        // Create two separate resource layouts
        // Set 0: Uniform buffer (updated per tile)
        _uniformLayout = _device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("TerrainUniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

        // Set 1: Heightmap texture + sampler (per tile, static)
        _textureLayout = _device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("HeightmapTexture", ResourceKind.TextureReadOnly, ShaderStages.Vertex | ShaderStages.Fragment),
            new ResourceLayoutElementDescription("HeightmapSampler", ResourceKind.Sampler, ShaderStages.Vertex | ShaderStages.Fragment)));

        // Create pipeline - array order determines GLSL location (0, 1, 2)
        // When using SPIRV, ALL elements use TextureCoordinate semantic
        // DO NOT specify offsets - let Veldrid calculate them from the formats
        VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),   // location 0
            new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),   // location 1
            new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));    // location 2

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
                shaders: _shaders!),
            Outputs = outputDescription ?? _device.SwapchainFramebuffer.OutputDescription
        };

        _pipeline = _device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);
        Console.WriteLine("[VeldridTerrainManager] Pipeline created with separate resource sets");
    }

    public void Dispose()
    {
        // Dispose background resources
        _backgroundResourceSet?.Dispose();
        _backgroundUniformBuffer?.Dispose();
        _backgroundUniformLayout?.Dispose();
        _backgroundMesh?.Dispose();
        _backgroundPipeline?.Dispose();
        if (_backgroundShaders != null)
        {
            foreach (var shader in _backgroundShaders)
                shader.Dispose();
        }

        // Dispose shared resources
        _textureResourceSet?.Dispose();
        _flatHeightmap?.Dispose();
        _sharedMesh?.Dispose();
        _pipeline?.Dispose();
        _uniformLayout?.Dispose();
        _textureLayout?.Dispose();
        _sampler?.Dispose();
        if (_shaders != null)
        {
            foreach (var shader in _shaders)
                shader.Dispose();
        }
    }
}

public struct Vector2Int : IEquatable<Vector2Int>
{
    public int X { get; set; }
    public int Y { get; set; }

    public Vector2Int(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2Int other && Equals(other);
    }

    public bool Equals(Vector2Int other)
    {
        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Vector2Int left, Vector2Int right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2Int left, Vector2Int right)
    {
        return !left.Equals(right);
    }

    public override string ToString() => $"({X}, {Y})";
}
