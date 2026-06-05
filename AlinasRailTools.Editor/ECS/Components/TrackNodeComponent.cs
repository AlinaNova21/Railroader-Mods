namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component containing game-specific data for track nodes.
/// Stores the unique identifier and any additional metadata.
/// </summary>
public struct TrackNodeComponent
{
    /// <summary>
    /// Unique identifier for this track node (from game data)
    /// </summary>
    public string Id;

    /// <summary>
    /// Node type or category (for future use)
    /// </summary>
    public string? Type;

    public TrackNodeComponent(string id, string? type = null)
    {
        Id = id;
        Type = type;
    }
}
