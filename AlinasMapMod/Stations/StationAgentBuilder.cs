using System;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Validation;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace AlinasMapMod.Stations;

public partial class StationAgentBuilder : SplineyBuilderBase, IObjectFactory
{
  public string Name => "Station Agent";
  public bool Enabled => true;
  public Type ObjectType => typeof(PaxStationAgent);

  protected override GameObject BuildSplineyInternal(string id, Transform parentTransform, JObject data)
  {
    return BuildFromCreatableComponent<PaxStationAgent, SerializedStationAgent>(id, data);
  }

  public IEditableObject CreateObject(PatchEditor editor, string id) => new SerializedStationAgent
  {
    Prefab = "empty://",
    PassengerStop = "whittier",
  }.Create(id);
}
