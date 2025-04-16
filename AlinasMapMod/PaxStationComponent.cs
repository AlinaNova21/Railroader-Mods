using System.Collections.Generic;
using System.Linq;
using Model.Ops;
using Model.Ops.Definition;
using Model.Ops.Timetable;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using StrangeCustoms.Tracks.Industries;
using Track;
using UnityEngine;

namespace AlinasMapMod;

public class PaxStationComponent : IndustryComponent, ICustomIndustryComponent
{
  public string TimetableCode { get; set; } = "";
  public int BasePopulation { get; set; } = 40;
  public string[] NeighborIds { get; set; } = [];
  public string Branch { get; set; } = "Main";
  public Load Load { get; set; }

  public void DeserializeComponent(SerializedComponent serializedComponent, PatchingContext ctx)
  {
    serializedComponent.ExtraData.TryGetValue("basePopulation", out var basePopulation);
    if (basePopulation != null)
    {
      BasePopulation = (int)basePopulation;
    }

    serializedComponent.ExtraData.TryGetValue("timetableCode", out var timetableCode);
    if (timetableCode != null)
    {
      TimetableCode = (string)timetableCode;
    }

    serializedComponent.ExtraData.TryGetValue("neighborIds", out var rawNeighborIds);
    if (rawNeighborIds != null)
    {
      NeighborIds = rawNeighborIds.Values<string>().ToArray();
    }

    serializedComponent.ExtraData.TryGetValue("branch", out var branch);
    if (branch != null)
    {
      Branch = (string)branch;
    }

    serializedComponent.ExtraData.TryGetValue("loadId", out var passengerLoad);
    passengerLoad ??= "passengers";

    Load = ctx.GetLoad((string)passengerLoad);
  }

  public void SerializeComponent(SerializedComponent serializedComponent)
  {
    serializedComponent.ExtraData["basePopulation"] = BasePopulation;
    serializedComponent.ExtraData["timetableCode"] = TimetableCode;
    serializedComponent.ExtraData["neighborIds"] = JToken.FromObject(NeighborIds);
    serializedComponent.ExtraData["branch"] = Branch;
    serializedComponent.ExtraData["loadId"] = Load.id;
  }

  public void OnEnable() {
    var paxStop = GetComponentInChildren<PassengerStop>();
    if (paxStop == null) {
      var go = new GameObject(this.name);
      go.SetActive(false);
      go.transform.parent = this.transform;
      paxStop = go.AddComponent<PassengerStop>();
    }
    paxStop.identifier = name;
    paxStop.passengerLoad = Load;
    paxStop.basePopulation = BasePopulation;
    paxStop.timetableCode = TimetableCode;
    paxStop.neighbors = NeighborIds.Select(neighborId => GameObject.FindObjectsOfType<PassengerStop>(true).First(stop => stop.identifier == neighborId)).ToArray();

    var ttc = GameObject.FindObjectOfType<TimetableController>();
    var branch = ttc.branches.FirstOrDefault(branch => branch.name == Branch);
    if (branch == null || branch == default) {
      branch = new TimetableBranch
      {
        name = Branch,
        stations = [],
      };
      ttc.branches.Add(branch);
    }
    if (!branch.stations.Exists(station => station.code == TimetableCode)) {
      var station = new TimetableStation
      {
        passengerStop = paxStop,
        code = TimetableCode,
        name = this.name,
      };
      branch.stations.Add(station);
    } else {
      var station = branch.stations.First(station => station.code == TimetableCode);
      station.code = TimetableCode;
      station.name = this.name;
      station.passengerStop = paxStop;
    }

    var existingSpans = paxStop.GetComponents<TrackSpan>();
    var spansToRemove = new HashSet<TrackSpan>(existingSpans);
    foreach(var span in this.TrackSpans) {
      TrackSpan ts = existingSpans.FirstOrDefault(ts => ts.id + ".pax" == span.id);
      if (ts == null || ts == default) {
        ts = paxStop.gameObject.AddComponent<TrackSpan>();
      } else {
        spansToRemove.Remove(span);
      }
      ts.id = span.id + ".pax";
      ts.upper = span.upper;
      ts.lower = span.lower;
    }
    foreach(var span in spansToRemove) {
      Destroy(span);
    }
    paxStop.gameObject.SetActive(true);
  }

  public override void Service(IIndustryContext ctx)
  {

  }

  public override void OrderCars(IIndustryContext ctx)
  {

  }
}
