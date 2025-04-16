using System;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using AlinasMapMod.Definitions;
using Helpers.Culling;
using Microsoft.SqlServer.Server;
using Model.Ops;
using Model.Ops.Definition;
using Newtonsoft.Json.Linq;
using RollingStock;
using RollingStock.Controls;
using Serilog;
using Track;
using UnityEngine;
using UnityEngine.UI;

namespace AlinasMapMod;

public class PaxBuilder : StrangeCustoms.ISplineyBuilder
{
  Serilog.ILogger logger = Log.ForContext<PaxBuilder>();
  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    logger.Warning("PaxBuilder Splineys are deprecated, use industry components instead. Id: ${id}");
    var pax = data.ToObject<SerializedPax>();
    logger.Information($"Configuring pax {id}");

    var spans = pax.SpanIds.Select(spanId => GameObject.FindObjectsOfType<TrackSpan>(true).First(span => span.id == spanId)).ToArray();
    if (pax.Industry == "")
    {
      throw new ArgumentException("Industry not set for pax " + id);
    }
    var industry = GameObject.FindObjectsByType<Industry>(FindObjectsSortMode.None).Single(v => v.identifier == pax.Industry);

    var go = new GameObject(industry.name);
    go.SetActive(false);
    go.transform.parent = industry.transform;
    var stop = go.AddComponent<PassengerStop>();
    stop.identifier = id;
    stop.passengerLoad = Model.CarPrototypeLibrary.instance.LoadForId("passengers");
    stop.basePopulation = pax.BasePopulation;
    stop.timetableCode = pax.TimetableCode;
    stop.neighbors = pax.NeighborIds.Select(neighborId => GameObject.FindObjectsOfType<PassengerStop>(true).First(stop => stop.identifier == neighborId)).ToArray();

    foreach (var span in spans)
    {
      var ts = go.AddComponent<TrackSpan>();
      ts.id = span.id + ".pax";
      ts.upper = span.upper;
      ts.lower = span.lower;
    }
    go.SetActive(true);
    return go;
  }
}

