using System.Collections.Generic;
using AlinasMapMod.Definitions;
using Newtonsoft.Json.Linq;
using Serilog;
using SimpleGraph.Runtime;
using StrangeCustoms.Tracks;
using TelegraphPoles;
using UnityEngine;

namespace AlinasMapMod;

public class TelegraphPoleMover : ISplineyBuilder
{
  public static Serilog.ILogger logger = Log.ForContext<TelegraphPoleMover>();
  private static HashSet<int> movedPoles = new();
  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var tpm = Object.FindObjectOfType<TelegraphPoleManager>();
    logger.Debug("Adjusting telegraph pole positions2");
    var g = tpm.GetComponent<SimpleGraph.Runtime.SimpleGraph>();

    var poles = data.ToObject<SerializedTelegraphPoles>();
    int i = 0;
    foreach (var poleId in poles.PolesToMove)
    {
      if (!movedPoles.Contains(poleId))
      {
        Node n = g.NodeForId(poleId);
        n.position += new Vector3(poles.PoleMovement[i,0], poles.PoleMovement[i,1], poles.PoleMovement[i,2]);
        movedPoles.Add(poleId);
      }
      i++;
    }

    return new GameObject();
  }
}
