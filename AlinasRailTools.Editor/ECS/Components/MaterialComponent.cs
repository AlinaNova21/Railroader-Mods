using System.Numerics;
using Veldrid;
using AlinasRailTools.Editor.Rendering;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component that references a Material for rendering
/// DEPRECATED: Use MeshRendererComponent instead
/// This component is kept for backward compatibility with existing code
/// </summary>
[Obsolete("Use MeshRendererComponent instead")]
public struct MaterialComponent
{
    /// <summary>
    /// Graphics pipeline (shaders, blend state, depth state, etc.)
    /// This is a shared resource - multiple entities can use the same pipeline.
    /// DEPRECATED: Pipeline is now managed by Material class
    /// </summary>
    public Pipeline Pipeline;

    /// <summary>
    /// Resource layout for uniforms
    /// DEPRECATED: Layout is now managed by Material class
    /// </summary>
    public ResourceLayout UniformLayout;

    /// <summary>
    /// Base color for the material (can be modulated per-instance)
    /// DEPRECATED: Color is now part of Material class
    /// </summary>
    public Vector4 Color;

    public MaterialComponent(Pipeline pipeline, ResourceLayout uniformLayout, Vector4 color)
    {
        Pipeline = pipeline;
        UniformLayout = uniformLayout;
        Color = color;
    }

    /// <summary>
    /// Create material with default white color
    /// </summary>
    public MaterialComponent(Pipeline pipeline, ResourceLayout uniformLayout)
        : this(pipeline, uniformLayout, new Vector4(1.0f, 1.0f, 1.0f, 1.0f))
    {
    }
}
