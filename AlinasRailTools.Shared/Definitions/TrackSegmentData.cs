namespace AlinasRailTools.Shared.Definitions;

/// <summary>
/// Database-backed track segment data
/// </summary>
public class TrackSegmentData
{
    public string Id { get; set; } = string.Empty;
    public string StartNodeId { get; set; } = string.Empty;
    public string EndNodeId { get; set; } = string.Empty;
    public string Type { get; set; }
}
