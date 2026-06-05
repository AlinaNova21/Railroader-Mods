using Arch.Core;

namespace AlinasRailTools.Editor.ECS.Components;

/// <summary>
/// Component representing a track segment connecting two nodes
/// </summary>
public struct TrackSegmentComponent
{
    /// <summary>
    /// Segment ID from game graph
    /// </summary>
    public string SegmentId;

    /// <summary>
    /// Entity reference to start node
    /// </summary>
    public Entity StartNodeEntity;

    /// <summary>
    /// Entity reference to end node
    /// </summary>
    public Entity EndNodeEntity;

    /// <summary>
    /// Whether this segment needs curve recalculation
    /// </summary>
    public bool IsDirty;

    public TrackSegmentComponent(string segmentId, Entity startNode, Entity endNode)
    {
        SegmentId = segmentId;
        StartNodeEntity = startNode;
        EndNodeEntity = endNode;
        IsDirty = true; // Start dirty so curve gets calculated
    }
}
