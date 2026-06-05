namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component that holds multiple MeshRendererComponents for different LOD levels
/// Used by LODSystem to toggle between different quality levels
/// </summary>
public struct MeshRendererCollectionComponent
{
    /// <summary>
    /// Array of renderers, indexed by LOD level
    /// Index 0 = highest quality, higher indices = lower quality
    /// </summary>
    public MeshRendererComponent[] Renderers;

    public MeshRendererCollectionComponent(params MeshRendererComponent[] renderers)
    {
        Renderers = renderers;
    }

    /// <summary>
    /// Get renderer for specific LOD level
    /// </summary>
    public MeshRendererComponent? GetRenderer(int lodLevel)
    {
        if (lodLevel >= 0 && lodLevel < Renderers.Length)
            return Renderers[lodLevel];
        return null;
    }

    /// <summary>
    /// Enable renderer at specific LOD level, disable all others
    /// </summary>
    public void SetActiveLevel(int lodLevel)
    {
        for (int i = 0; i < Renderers.Length; i++)
        {
            Renderers[i].Enabled = (i == lodLevel);
        }
    }
}
