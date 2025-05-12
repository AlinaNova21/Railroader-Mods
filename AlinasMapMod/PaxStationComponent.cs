using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers;
using Model.Ops;
using Model.Ops.Definition;
using Model.Ops.Timetable;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using StrangeCustoms.Tracks.Industries;
using Track;
using UnityEngine;

namespace AlinasMapMod;

public class PaxStationComponent : IndustryComponent, ICustomIndustryComponent, IIndustryTrackDisplayable
{
  private Serilog.ILogger logger = Log.ForContext(typeof(PaxStationComponent));
  public string TimetableCode { get; set; } = "";
  public int BasePopulation { get; set; } = 40;
  public string[] NeighborIds { get; set; } = [];
  public string Branch { get; set; } = "Main";
  public Load Load { get; set; }

  public override bool IsVisible => false;

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
    Messenger.Default.Register<GraphDidRebuildCollections>(this, UpdatePax);
    logger.Information("PaxStationComponent {name} OnEnable", this.name);
  }

  public void OnDisable()
  {
    Messenger.Default.Unregister<GraphDidRebuildCollections>(this, UpdatePax);
  }

  private void UpdatePax(GraphDidRebuildCollections collections)
  {
    var paxStop = transform.parent.GetComponentInChildren<PassengerStop>();
    var wasActive = paxStop != null && paxStop.gameObject.activeSelf;
    if (paxStop == null) {
      logger.Information("PaxStop {name} does not exist, creating", this.name);
      var go = new GameObject(this.name);
      wasActive = true;
      go.SetActive(false);
      go.transform.parent = this.transform.parent;
      paxStop = go.AddComponent<PassengerStop>();
    } else
    {
      logger.Information("PaxStop {name} already exists active: {wasActive}, updating", paxStop.name, wasActive);
    }
    paxStop.gameObject.SetActive(false);
    paxStop.transform.DestroyAllChildren();
    paxStop.identifier = subIdentifier;
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

    if (wasActive && !paxStop.gameObject.activeSelf)
      paxStop.gameObject.SetActive(true);
  }

  public override void Service(IIndustryContext ctx)
  {

  }

  public override void OrderCars(IIndustryContext ctx)
  {

  }
}
