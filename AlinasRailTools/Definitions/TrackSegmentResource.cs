using AlinasRailTools.Core.Attributes;

namespace AlinasRailTools.Definitions;

/// <summary>
/// Track visual styles supported by the game
/// </summary>
public enum TrackStyle
{
    Standard,
    Bridge,
    Tunnel,
    Yard
}

/// <summary>
/// Track classifications for operational purposes  
/// </summary>
public enum TrackClass
{
    Mainline,
    Branch,
    Industrial
}

/// <summary>
/// Represents a track segment connecting two track nodes.
/// Track segments depend on TrackNodeResources for their StartId and EndId references.
/// </summary>
[ResourceType("trackSegments")]
public class TrackSegmentResource : BaseResource
{
    /// <summary>
    /// ID of the starting track node
    /// </summary>
    [Required]
    [ResourceReference(typeof(TrackNodeResource))]
    public string StartId { get; set; } = "";

    /// <summary>
    /// ID of the ending track node
    /// </summary>
    [Required]
    [ResourceReference(typeof(TrackNodeResource))]
    public string EndId { get; set; } = "";

    /// <summary>
    /// Visual style/type of the track 
    /// </summary>
    public TrackStyle Style { get; set; } = TrackStyle.Standard;

    /// <summary>
    /// Track classification for operational purposes
    /// </summary>
    public TrackClass TrackClass { get; set; } = TrackClass.Mainline;

    /// <summary>
    /// Speed limit for this segment in miles per hour (0 = no limit)
    /// </summary>
    [Range(0, 200)]
    public int SpeedLimit { get; set; } = 0;

    /// <summary>
    /// Priority for track selection when multiple segments connect the same nodes
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Group identifier for related track segments
    /// </summary>
    public string GroupId { get; set; } = "";
}