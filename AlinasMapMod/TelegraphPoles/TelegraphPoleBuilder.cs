using System;
using System.Collections.Generic;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Validation;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Newtonsoft.Json.Linq;
using Serilog;
using SimpleGraph.Runtime;
using StrangeCustoms;
using StrangeCustoms.Tracks;
using TelegraphPoles;
using UnityEngine;

namespace AlinasMapMod.TelegraphPoles;

public class TelegraphPoleBuilder : SplineyBuilderBase
{
  
  private static HashSet<int> raisedPoles = new();

  public TelegraphPoleBuilder()
  {
    Logger.Debug("TelegraphPoleBuilder loaded");
    Messenger.Default.Register(this, new Action<MapDidUnloadEvent>(HandleMapUnloaded));
  }

  private void HandleMapUnloaded(MapDidUnloadEvent ev)
  {
    Logger.Debug("Clearing raised poles");
    raisedPoles.Clear();
  }

  protected override GameObject BuildSplineyInternal(string id, Transform parentTransform, JObject data)
  {
    Logger.Information($"Processing telegraph poles {id}");
    
    // Use validation framework to deserialize and validate
    var poles = DeserializeAndValidate<SerializedTelegraphPoles>(data);
    
    var tpm = UnityEngine.Object.FindObjectOfType<TelegraphPoleManager>();
    Logger.Debug("Adjusting telegraph pole positions");
    var g = tpm.GetComponent<SimpleGraph.Runtime.SimpleGraph>();

    foreach (var poleId in poles.PolesToRaise) {
      if (!raisedPoles.Contains(poleId)) {
        Node n = g.NodeForId(poleId);
        n.position += new Vector3(0, 2, 0);
        raisedPoles.Add(poleId);
      }
    }

    return new GameObject();
  }
}
