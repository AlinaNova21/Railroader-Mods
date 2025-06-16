using System;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace AlinasMapMod.Stations;

public partial class StationAgentBuilder : StrangeCustoms.ISplineyBuilder, IObjectFactory
{
  readonly Serilog.ILogger logger = Log.ForContext<StationAgentBuilder>();

  public bool Enabled => true;

  public string Name => "Station Agent";

  public Type ObjectType => typeof(PaxStationAgent);

  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var agent = data.ToObject<SerializedStationAgent>();
    logger.Information($"Configuring Station Agent {id} with prefab {agent.Prefab}");
    try {
      agent.Validate();
      return agent.Create(id).gameObject;
    } catch (ValidationException ex) {
      logger.Error(ex, "Validation failed for Station Agent {Id}", id);
      throw new ValidationException($"Validation failed for Station Agent {id}: {ex.Message}");
    } catch (Exception ex) {
      logger.Error(ex, "Failed to create Station Agent {Id}", id);
      throw new InvalidOperationException($"Failed to create Station Agent {id}", ex);
    }
  }

  public IEditableObject CreateObject(PatchEditor editor, string id) => new SerializedStationAgent
  {
    Prefab = "empty://",
    PassengerStop = "whittier",
  }.Create(id);
}
