using Arch.Core;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component for Level of Detail management
/// Defines distance thresholds for switching between LOD levels
/// Can optionally reference child entities for each LOD level
/// </summary>
public struct LODComponent
{
    /// <summary>
    /// Distance thresholds for each LOD level (in meters)
    /// LOD 0 is active from 0 to Thresholds[0]
    /// LOD 1 is active from Thresholds[0] to Thresholds[1]
    /// LOD n is active from Thresholds[n-1] to Thresholds[n]
    /// Last LOD is active from Thresholds[last] to infinity
    /// </summary>
    public float[] Thresholds;

    /// <summary>
    /// Optional child entities for each LOD level
    /// If set, LODSystem will toggle RenderableComponent on these entities
    /// Length should be Thresholds.Length + 1 (one entity per LOD level)
    /// </summary>
    public Entity[]? ChildEntities;

    /// <summary>
    /// Currently active LOD level (updated by LODSystem)
    /// </summary>
    public int CurrentLevel;

    /// <summary>
    /// Create LOD component with distance thresholds
    /// </summary>
    /// <param name="thresholds">Distance thresholds in ascending order</param>
    public LODComponent(params float[] thresholds)
    {
        Thresholds = thresholds;
        ChildEntities = null;
        CurrentLevel = 0;
    }

    /// <summary>
    /// Create LOD component with distance thresholds and child entities
    /// </summary>
    /// <param name="thresholds">Distance thresholds in ascending order</param>
    /// <param name="childEntities">Child entities for each LOD level</param>
    public LODComponent(float[] thresholds, Entity[] childEntities)
    {
        Thresholds = thresholds;
        ChildEntities = childEntities;
        CurrentLevel = 0;
    }

    /// <summary>
    /// Create LOD component with two levels (detailed and simple)
    /// </summary>
    public static LODComponent TwoLevel(float switchDistance)
    {
        return new LODComponent(switchDistance);
    }

    /// <summary>
    /// Create LOD component with three levels (high, medium, low)
    /// </summary>
    public static LODComponent ThreeLevel(float mediumDistance, float lowDistance)
    {
        return new LODComponent(mediumDistance, lowDistance);
    }

    /// <summary>
    /// Calculate which LOD level should be active for a given distance
    /// </summary>
    public int GetLevelForDistance(float distance)
    {
        for (int i = 0; i < Thresholds.Length; i++)
        {
            if (distance < Thresholds[i])
                return i;
        }
        return Thresholds.Length; // Last level (lowest quality)
    }
}
