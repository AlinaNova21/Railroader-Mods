using JsonSubTypes;
using Newtonsoft.Json;

namespace AlinasRailTools.Shared.Definitions;

/// <summary>
/// Base class for all industry component serialization
/// Uses JsonSubTypes for discriminator-based polymorphic deserialization
/// Discriminator uses full namespace paths to distinguish base game vs mods
/// </summary>
[JsonConverter(typeof(JsonSubtypes), "type")]
// Base game types (Model.Ops namespace)
[JsonSubtypes.KnownSubType(typeof(SerializedIndustryLoader), "Model.Ops.IndustryLoader")]
[JsonSubtypes.KnownSubType(typeof(SerializedInterchangedIndustryLoader), "Model.Ops.InterchangedIndustryLoader")]
[JsonSubtypes.KnownSubType(typeof(SerializedLoadExporter), "Model.Ops.LoadExporter")]
[JsonSubtypes.KnownSubType(typeof(SerializedLoadImporter), "Model.Ops.LoadImporter")]
[JsonSubtypes.KnownSubType(typeof(SerializedIndustryUnloader), "Model.Ops.IndustryUnloader")]
[JsonSubtypes.KnownSubType(typeof(SerializedFormulaicIndustryComponent), "Model.Ops.FormulaicIndustryComponent")]
[JsonSubtypes.KnownSubType(typeof(SerializedProgressionIndustryComponent), "Model.Ops.ProgressionIndustryComponent")]
[JsonSubtypes.KnownSubType(typeof(SerializedInterchange), "Model.Ops.Interchange")]
[JsonSubtypes.KnownSubType(typeof(SerializedTeamTrack), "Model.Ops.TeamTrack")]
[JsonSubtypes.KnownSubType(typeof(SerializedRepairTrack), "Model.Ops.RepairTrack")]
[JsonSubtypes.KnownSubType(typeof(SerializedTeleportLoadingIndustry), "Model.Ops.TeleportLoadingIndustry")]
// Mod types (can add more with different namespaces)
// Note: PassengerStop was originally added by AlinasMapMod but is now a base game type
[JsonSubtypes.KnownSubType(typeof(SerializedPassengerStop), "Model.Ops.PassengerStop")]
public class SerializedIndustryComponent
{
  [JsonProperty("type")]
  public string Type { get; set; } = "";

  [JsonProperty("subIdentifier")]
  public string SubIdentifier { get; set; } = "";
}

/// <summary>
/// Base class for all loader-type components (mirrors IndustryLoaderBase)
/// </summary>
public abstract class SerializedIndustryLoaderBase : SerializedIndustryComponent
{
  [JsonProperty("load")]
  public string Load { get; set; } = "";

  [JsonProperty("productionRate")]
  public float ProductionRate { get; set; }

  [JsonProperty("maxStorage")]
  public float MaxStorage { get; set; }

  [JsonProperty("orderEmpties")]
  public bool OrderEmpties { get; set; }
}

/// <summary>
/// Standard industry loader
/// </summary>
public class SerializedIndustryLoader : SerializedIndustryLoaderBase
{
  // IndustryLoader has no additional properties beyond base
}

/// <summary>
/// Loader that loads cars for interchange to other industries
/// </summary>
public class SerializedInterchangedIndustryLoader : SerializedIndustryLoaderBase
{
  [JsonProperty("targetIndustries")]
  public string[] TargetIndustries { get; set; } = [];
}

/// <summary>
/// Loader that exports loads from the map
/// </summary>
public class SerializedLoadExporter : SerializedIndustryLoaderBase
{
  // LoadExporter has no additional properties beyond base
}

/// <summary>
/// Loader that imports loads to the map
/// </summary>
public class SerializedLoadImporter : SerializedIndustryLoaderBase
{
  // LoadImporter has no additional properties beyond base
}

/// <summary>
/// Industry unloader component
/// </summary>
public class SerializedIndustryUnloader : SerializedIndustryComponent
{
  [JsonProperty("load")]
  public string Load { get; set; } = "";

  [JsonProperty("carUnloadRate")]
  public float CarUnloadRate { get; set; }

  [JsonProperty("storageConsumptionRate")]
  public float StorageConsumptionRate { get; set; }

  [JsonProperty("maxStorage")]
  public float MaxStorage { get; set; }

  [JsonProperty("orderAwayEmpties")]
  public bool OrderAwayEmpties { get; set; }

  [JsonProperty("orderLoads")]
  public bool OrderLoads { get; set; }
}

/// <summary>
/// Formulaic industry component (formula-driven production)
/// </summary>
public class SerializedFormulaicIndustryComponent : SerializedIndustryComponent
{
  // TODO: Add formulaic-specific properties
}

/// <summary>
/// Progression-based industry component (unlocked by progression)
/// </summary>
public class SerializedProgressionIndustryComponent : SerializedIndustryComponent
{
  // TODO: Add progression-specific properties
}

/// <summary>
/// Interchange component (handles car exchanges between industries)
/// </summary>
public class SerializedInterchange : SerializedIndustryComponent
{
  // TODO: Add interchange-specific properties
}

/// <summary>
/// Team track component (player-accessible storage track)
/// </summary>
public class SerializedTeamTrack : SerializedIndustryComponent
{
  [JsonProperty("profileName")]
  public string ProfileName { get; set; } = "";
}

/// <summary>
/// Repair track component (car repair facility)
/// </summary>
public class SerializedRepairTrack : SerializedIndustryComponent
{
  // TODO: Add repair-track-specific properties
}

/// <summary>
/// Teleport loading component (instant load/unload)
/// </summary>
public class SerializedTeleportLoadingIndustry : SerializedIndustryComponent
{
  // TODO: Add teleport-loading-specific properties
}

/// <summary>
/// Passenger stop component
/// </summary>
public class SerializedPassengerStop : SerializedIndustryComponent
{
  [JsonProperty("identifier")]
  public string Identifier { get; set; } = "";

  [JsonProperty("displayName")]
  public string DisplayName { get; set; } = "";

  [JsonProperty("position")]
  public SerializedVector3 Position { get; set; } = new();
}
