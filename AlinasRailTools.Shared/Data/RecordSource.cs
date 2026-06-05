namespace AlinasRailTools.Shared.Data;

/// <summary>
/// Indicates the source of a database record
/// </summary>
public enum RecordSource
{
    /// <summary>Record was loaded from existing world state</summary>
    FromWorld,

    /// <summary>Record was imported from JSON file</summary>
    FromJson,

    /// <summary>Record was created programmatically</summary>
    Created
}
