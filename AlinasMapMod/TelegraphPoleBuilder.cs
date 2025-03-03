using System;
using System.Collections.Generic;
using AlinasMapMod.Definitions;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Newtonsoft.Json.Linq;
using Serilog;
using SimpleGraph.Runtime;
using StrangeCustoms;
using TelegraphPoles;
using UnityEngine;

namespace AlinasMapMod;

public class TelegraphPoleBuilder : ISplineyBuilder
{
  public static Serilog.ILogger logger = Log.ForContext<TelegraphPoleBuilder>();
  private static HashSet<int> raisedPoles = new();

  public TelegraphPoleBuilder()
  {
    logger.Debug("TelegraphPoleBuilder loaded");
    Messenger.Default.Register(this, new Action<MapDidUnloadEvent>(HandleMapUnloaded));
  }

  private void HandleMapUnloaded(MapDidUnloadEvent ev)
  {
    logger.Debug("Clearing raised poles");
    raisedPoles.Clear();
  }

  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var tpm = UnityEngine.Object.FindObjectOfType<TelegraphPoleManager>();
    logger.Debug("Adjusting telegraph pole positions");
    var g = tpm.GetComponent<SimpleGraph.Runtime.SimpleGraph>();

    var poles = data.ToObject<SerializedTelegraphPoles>();

    foreach (var poleId in poles.PolesToRaise)
    {
      if (!raisedPoles.Contains(poleId))
      {
        Node n = g.NodeForId(poleId);
        n.position += new Vector3(0, 2, 0);
        raisedPoles.Add(poleId);
      }
    }

    return new GameObject();
  }
}
