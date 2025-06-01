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

namespace AlinasMapMod.TelegraphPoles;

public class TelegraphPoleMover : ISplineyBuilder
{
  public static Serilog.ILogger logger = Log.ForContext<TelegraphPoleMover>();
  private static HashSet<int> movedPoles = new();
  public TelegraphPoleMover()
  {
    logger.Debug("TelegraphPoleMover loaded");
    Messenger.Default.Register(this, new System.Action<MapDidUnloadEvent>(HandleMapUnloaded));
  }

  private void HandleMapUnloaded(MapDidUnloadEvent ev)
  {
    logger.Debug("Clearing raised poles");
    movedPoles.Clear();
  }

  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var tpm = Object.FindObjectOfType<TelegraphPoleManager>();
    logger.Debug("Adjusting telegraph pole positions2");
    var g = tpm.GetComponent<SimpleGraph.Runtime.SimpleGraph>();

    var poles = data.ToObject<SerializedTelegraphPoles>();
    int i = 0;
    foreach (var poleId in poles.PolesToMove) {
      if (!movedPoles.Contains(poleId)) {
        Node n = g.NodeForId(poleId);
        n.position += new Vector3(poles.PoleMovement[i, 0], poles.PoleMovement[i, 1], poles.PoleMovement[i, 2]);
        movedPoles.Add(poleId);
      }
      i++;
    }

    return new GameObject();
  }
}
