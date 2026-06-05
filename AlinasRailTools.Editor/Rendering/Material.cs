using System.Numerics;
using Veldrid;

namespace AlinasRailTools.Editor.Rendering;

/// <summary>
/// Material definition containing shader references and pipeline configuration
/// Materials can be shared across multiple entities
/// </summary>
public class Material
{
    /// <summary>
    /// Vertex shader resource name (embedded resource path)
    /// </summary>
    public string VertexShader { get; set; }

    /// <summary>
    /// Fragment shader resource name (embedded resource path)
    /// </summary>
    public string FragmentShader { get; set; }

    /// <summary>
    /// Base color for the material
    /// </summary>
    public Vector4 Color { get; set; }

    /// <summary>
    /// Whether this material supports GPU instancing
    /// If true, the vertex shader must accept instance data (4x vec4 transform matrix)
    /// </summary>
    public bool SupportsInstancing { get; set; }

    /// <summary>
    /// Blend state configuration
    /// </summary>
    public BlendStateDescription BlendState { get; set; }

    /// <summary>
    /// Depth/stencil state configuration
    /// </summary>
    public DepthStencilStateDescription DepthStencilState { get; set; }

    /// <summary>
    /// Rasterizer state configuration
    /// </summary>
    public RasterizerStateDescription RasterizerState { get; set; }

    /// <summary>
    /// Primitive topology (triangle list, line list, etc.)
    /// </summary>
    public PrimitiveTopology PrimitiveTopology { get; set; }

    /// <summary>
    /// Cached pipeline instance (created lazily on first use)
    /// </summary>
    public Pipeline? CachedPipeline { get; set; }

    /// <summary>
    /// Cached resource layout (created with pipeline)
    /// </summary>
    public ResourceLayout? CachedUniformLayout { get; set; }

    /// <summary>
    /// Create a material with default settings
    /// </summary>
    public Material(string vertexShader, string fragmentShader)
    {
        VertexShader = vertexShader;
        FragmentShader = fragmentShader;
        Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        SupportsInstancing = false;

        // Default states (standard opaque rendering)
        BlendState = BlendStateDescription.SingleAlphaBlend;
        DepthStencilState = new DepthStencilStateDescription(
            depthTestEnabled: true,
            depthWriteEnabled: true,
            comparisonKind: ComparisonKind.LessEqual);
        RasterizerState = new RasterizerStateDescription(
            cullMode: FaceCullMode.Back,
            fillMode: PolygonFillMode.Solid,
            frontFace: FrontFace.CounterClockwise,
            depthClipEnabled: true,
            scissorTestEnabled: false);
        PrimitiveTopology = PrimitiveTopology.TriangleList;
    }

    /// <summary>
    /// Create a colored material (uses common_color shaders)
    /// </summary>
    public static Material CreateColored(Vector4 color, bool supportsInstancing = false)
    {
        return new Material("tie_instanced.vs", "common_color.fs")
        {
            Color = color,
            SupportsInstancing = supportsInstancing
        };
    }

    /// <summary>
    /// Create a terrain material
    /// </summary>
    public static Material CreateTerrain()
    {
        return new Material("terrain.vs", "terrain.fs")
        {
            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            SupportsInstancing = false
        };
    }

    /// <summary>
    /// Create an instanced terrain material
    /// </summary>
    public static Material CreateInstancedTerrain()
    {
        return new Material("terrain_instanced.vs", "terrain.fs")
        {
            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            SupportsInstancing = true
        };
    }

    /// <summary>
    /// Create a material with alpha blending enabled
    /// </summary>
    public static Material CreateTransparent(string vertexShader, string fragmentShader, Vector4 color)
    {
        var material = new Material(vertexShader, fragmentShader)
        {
            Color = color
        };

        // Enable alpha blending
        material.BlendState = BlendStateDescription.SingleAlphaBlend;

        // Disable depth writes for transparency (but keep depth testing)
        material.DepthStencilState = new DepthStencilStateDescription(
            depthTestEnabled: true,
            depthWriteEnabled: false,
            comparisonKind: ComparisonKind.LessEqual);

        return material;
    }

    /// <summary>
    /// Clone this material with a different color
    /// </summary>
    public Material WithColor(Vector4 color)
    {
        return new Material(VertexShader, FragmentShader)
        {
            Color = color,
            SupportsInstancing = SupportsInstancing,
            BlendState = BlendState,
            DepthStencilState = DepthStencilState,
            RasterizerState = RasterizerState,
            PrimitiveTopology = PrimitiveTopology
        };
    }

    /// <summary>
    /// Clear cached pipeline (forces recreation on next use)
    /// Call this if device resources need to be recreated
    /// </summary>
    public void ClearCache()
    {
        CachedPipeline?.Dispose();
        CachedPipeline = null;
        CachedUniformLayout?.Dispose();
        CachedUniformLayout = null;
    }

    /// <summary>
    /// Dispose cached resources
    /// </summary>
    public void Dispose()
    {
        ClearCache();
    }
}
