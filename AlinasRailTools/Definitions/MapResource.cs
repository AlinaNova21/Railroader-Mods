using System.Collections.Generic;
using AlinasRailTools.Core.Attributes;

namespace AlinasRailTools.Definitions;

/// <summary>
/// Represents a map definition that updates menus and defines starting parameters.
/// Maps are global-only resources loaded early to update menus.
/// </summary>
[ResourceType("maps")]
public class MapResource : BaseResource
{
    /// <summary>
    /// Display name of the map
    /// </summary>
    [Required]
    public string Name { get; set; } = "";

    /// <summary>
    /// Brief description of the map
    /// </summary>
    [Required]
    public string Description { get; set; } = "";

    /// <summary>
    /// Difficulty level (e.g., "Beginner", "Intermediate", "Advanced")
    /// </summary>
    public string Difficulty { get; set; } = "Intermediate";

    /// <summary>
    /// Starting year for the map
    /// </summary>
    [Range(1800, 2100)]
    public int StartingYear { get; set; } = 1950;

    /// <summary>
    /// Starting money amount
    /// </summary>
    [Range(0, 10000000)]
    public int StartingMoney { get; set; } = 50000;

    /// <summary>
    /// Maximum number of players supported
    /// </summary>
    [Range(1, 16)]
    public int MaxPlayers { get; set; } = 4;

    /// <summary>
    /// Whether weather effects are enabled
    /// </summary>
    public bool WeatherEnabled { get; set; } = true;

    /// <summary>
    /// Whether seasonal changes are enabled
    /// </summary>
    public bool SeasonalChanges { get; set; } = true;

    /// <summary>
    /// Starting locomotive roster
    /// </summary>
    public List<StartingLocomotive> StartingLocomotives { get; set; } = new();

    /// <summary>
    /// Starting car inventory
    /// </summary>
    public List<StartingCar> StartingCars { get; set; } = new();
}

/// <summary>
/// Represents a starting locomotive configuration
/// </summary>
public class StartingLocomotive
{
    /// <summary>
    /// Locomotive type identifier
    /// </summary>
    [Required]
    public string Type { get; set; } = "";

    /// <summary>
    /// Livery/paint scheme identifier
    /// </summary>
    public string Livery { get; set; } = "default";

    /// <summary>
    /// Initial condition (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public float Condition { get; set; } = 0.8f;
}

/// <summary>
/// Represents a starting car inventory entry
/// </summary>
public class StartingCar
{
    /// <summary>
    /// Car type identifier
    /// </summary>
    [Required]
    public string Type { get; set; } = "";

    /// <summary>
    /// Number of cars of this type
    /// </summary>
    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;
}