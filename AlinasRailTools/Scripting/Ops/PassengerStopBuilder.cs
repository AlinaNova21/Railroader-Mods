using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using AlinasRailTools.Scripting;
using AlinasRailTools.Scripting.Graph;
using HarmonyLib;
using Model;
using Model.Ops;
using Model.Ops.Definition;
using Serilog;
using TMPro;
using Track;
using UnityEngine;

namespace AlinasRailTools.Scripting.Ops;

public class PassengerStopBuilder : BaseBuilder<IndustryBuilder, PassengerStopBuilder>
{
  public PassengerStop PassengerStop { get; }
  public Helper Helper => Parent.Helper;

  public PassengerStopBuilder(IndustryBuilder parent, PassengerStop passengerStop)
    : base(parent, passengerStop.identifier) => PassengerStop = passengerStop;

  public override IndustryBuilder Remove() => Parent.RemovePassengerStop(Id);

  public override PassengerStopBuilder Invalidate()
  {
    Parent.Invalidate();
    return this;
  }

  // Properties for PassengerStop fields
  public string TimetableCode { get => PassengerStop.timetableCode; set => PassengerStop.timetableCode = value; }
  public int BasePopulation { get => PassengerStop.basePopulation; set => PassengerStop.basePopulation = value; }
  public bool FlagStop { get => PassengerStop.flagStop; set => PassengerStop.flagStop = value; }
  public PassengerStop[] Neighbors { get => PassengerStop.neighbors; set => PassengerStop.neighbors = value; }
  public Load PassengerLoad { get => PassengerStop.passengerLoad; set => PassengerStop.passengerLoad = value; }
  public string Name { get => PassengerStop.name; set { PassengerStop.name = value; } }

  public TrackSpan[] TrackSpans
  {
    get
    {
      var field = typeof(PassengerStop).GetField("_spans", BindingFlags.NonPublic | BindingFlags.Instance);
      return (TrackSpan[])field?.GetValue(PassengerStop);
    }
    set
    {
      // Reparent the spans to this PassengerStop
      foreach (var span in value)
      {
        span.transform.SetParent(PassengerStop.transform);
      }

      var field = typeof(PassengerStop).GetField("_spans", BindingFlags.NonPublic | BindingFlags.Instance);
      field?.SetValue(PassengerStop, value);
      Invalidate();
    }
  }

  // Fluent methods
  public PassengerStopBuilder WithTimetableCode(string timetableCode) { TimetableCode = timetableCode; return this; }
  public PassengerStopBuilder WithBasePopulation(int basePopulation) { BasePopulation = basePopulation; return this; }
  public PassengerStopBuilder WithFlagStop(bool flagStop = true) { FlagStop = flagStop; return this; }
  public PassengerStopBuilder WithNeighbors(params PassengerStop[] neighbors) { Neighbors = neighbors; return this; }
  public PassengerStopBuilder WithNeighbors(params PassengerStopBuilder[] neighborBuilders) => WithNeighbors(neighborBuilders.Select(b => b.PassengerStop).ToArray());
  public PassengerStopBuilder WithPassengerLoad(Load load) { PassengerLoad = load; return this; }
  public PassengerStopBuilder WithTrackSpans(params TrackSpan[] trackSpans) { TrackSpans = trackSpans; return this; }
  public PassengerStopBuilder WithTrackSpans(params TrackSpanBuilder[] trackSpanBuilders) => WithTrackSpans(trackSpanBuilders.Select(b => b.Span).ToArray());
  public PassengerStopBuilder WithName(string name) { Name = name; return this; }
  public override PassengerStopBuilder SetActive(bool active = true) { PassengerStop.gameObject.SetActive(active); return this; }
  public PassengerStopBuilder PlaceStationAgent(TrackSpanBuilder trackSpan, PrefabTemplate template, float distance = 10f, bool flip = false) => PlaceStationAgent(trackSpan.Span, template, distance);
  public PassengerStopBuilder PlaceStationAgent(TrackSpan trackSpan, PrefabTemplate template, float distance = 10f, bool flip = false)
  {
    var center = trackSpan.GetCenterPoint();
    var half = trackSpan.Length / 2;
    var segments = trackSpan.GetSegments();
    // Find the segment that contains the center point
    Location? loc = null;
    foreach (var seg in segments)
    {
      loc = seg.LocationFromPoint(center, half);
      if (loc != null) break;
    }
    if (loc == null) throw new System.Exception("Could not find location on track span to place station agent");
    var agent = Helper.Prefabs
      .Instance(Id)
        .WithTemplate(template)
        .WithParentObject(PassengerStop.transform)
        .PlaceNextToTrack(loc.Value, flip, distance);
    var stationAgent = agent.Instance.GetComponentInChildren<StationAgent>(true);
    typeof(StationAgent).GetField("area", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
      ?.SetValue(stationAgent, Parent.Area.Area);
    typeof(StationAgent).GetField("passengerStop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
      ?.SetValue(stationAgent, PassengerStop);
    var secondaryAreas = AccessTools.Field(typeof(StationAgent), "secondaryAreas").GetValue(stationAgent) as List<Area>;
    secondaryAreas.Clear();

    var signs = agent.Instance.GetComponentsInChildren<TextMeshPro>(true);
    foreach (var sign in signs) {
      if (sign.transform.parent.name.StartsWith("Sign-Station")) sign.text = Parent.Area.Area.name;
    }

    foreach (Renderer r in agent.Instance.transform.GetComponentsInChildren<Renderer>()) {
      r.enabled = true; // enable all renderers
    }
    agent.Instance.SetActive(true);

    return this;
  }
}