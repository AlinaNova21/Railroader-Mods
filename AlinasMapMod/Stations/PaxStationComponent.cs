using System;
using System.Linq;
using AlinasMapMod.Caches;
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
using UnityEngine;

namespace AlinasMapMod.Stations;

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
    if (basePopulation != null) BasePopulation = (int)basePopulation;

    serializedComponent.ExtraData.TryGetValue("timetableCode", out var timetableCode);
    if (timetableCode != null) TimetableCode = (string)timetableCode;

    serializedComponent.ExtraData.TryGetValue("neighborIds", out var rawNeighborIds);
    if (rawNeighborIds != null) NeighborIds = rawNeighborIds.Values<string>().ToArray();

    serializedComponent.ExtraData.TryGetValue("branch", out var branch);
    if (branch != null) Branch = (string)branch;

    serializedComponent.ExtraData.TryGetValue("loadId", out var passengerLoad);
    passengerLoad ??= "passengers";

    Load = ctx.GetLoad((string)passengerLoad);

    try {
      Validate();
    } catch (Exception ex) {
      logger.Error(ex, "PaxStationComponent {name} validation failed", name);
      //throw ex;
    }
  }

  public void SerializeComponent(SerializedComponent serializedComponent)
  {
    serializedComponent.ExtraData["basePopulation"] = BasePopulation;
    serializedComponent.ExtraData["timetableCode"] = TimetableCode;
    serializedComponent.ExtraData["neighborIds"] = JToken.FromObject(NeighborIds);
    serializedComponent.ExtraData["branch"] = Branch;
    serializedComponent.ExtraData["loadId"] = Load.id;
  }

  public void OnEnable()
  {
    Messenger.Default.Register<GraphDidRebuildCollections>(this, UpdatePax);
    logger.Information("PaxStationComponent {name} OnEnable", name);
  }

  public void OnDisable()
  {
    Messenger.Default.Unregister<GraphDidRebuildCollections>(this, UpdatePax);
  }

  private bool Validate()
  {
    var paxStop = transform.parent.GetComponentInChildren<PassengerStop>(true);
    if (paxStop != null && paxStop.identifier != subIdentifier) {
      var msg = $"Existing station ({paxStop.identifier}) found for PaxStationComponent {subIdentifier}, but ids do not match, ({subIdentifier} != {paxStop.identifier})";
      DisplayError(msg);
      return false;
    }
    if (paxStop != null && paxStop.timetableCode != TimetableCode) {
      var msg = $"Existing station (${paxStop.identifier})) found but timetable codes do not match, ({TimetableCode} != {paxStop.timetableCode})";
      DisplayError(msg);
      return false;
    }
    var areaPaxStops = transform.parent.parent.GetComponentsInChildren<PassengerStop>(true);
    var matchingStop = areaPaxStops.SingleOrDefault(p => p.identifier == subIdentifier);
    if (matchingStop != null) {
      var myIndustry = GetComponentInParent<Industry>(true);
      var stopIndustry = matchingStop.GetComponentInParent<Industry>(true);
      if (myIndustry != stopIndustry) {
        var msg = $"Existing station {matchingStop.identifier} found for PaxStationComponent {subIdentifier}, but in another industry ({stopIndustry.identifier}). ({myIndustry.identifier} != {stopIndustry.identifier})";
        DisplayError(msg);
        return false;
      }
    }
    return true;
  }

  private void DisplayError(string msg)
  {
    logger.Error(msg);
    var console = UI.Console.Console.shared;
    if (console != null) {
      console.AddLine($"Error occurred for PaxStationComponent {subIdentifier}:");
      console.AddLine(msg);
    }
  }

  private void UpdatePax(GraphDidRebuildCollections collections)
  {
    try {
      if (!Validate()) return;
    } catch (InvalidOperationException ex) {
      logger.Error(ex, "PaxStationComponent {name} validation failed", name);
      return;
    }
    var paxStop = transform.parent.GetComponentInChildren<PassengerStop>();
    var wasActive = paxStop != null && paxStop.gameObject.activeSelf;
    if (paxStop == null) {
      logger.Information("PaxStop {name} does not exist, creating", name);
      var go = new GameObject(name);
      wasActive = true;
      go.SetActive(false);
      go.transform.parent = transform.parent;
      paxStop = go.AddComponent<PassengerStop>();
    } else {
      logger.Information("PaxStop {name} already exists active: {wasActive}, updating", paxStop.name, wasActive);
      if (paxStop.identifier != subIdentifier) throw new InvalidOperationException($"PaxStationComponent id does not match existing passenger stop, ({subIdentifier} != {paxStop.identifier})");
    }
    paxStop.gameObject.SetActive(false);
    paxStop.transform.DestroyAllChildren();
    paxStop.identifier = subIdentifier;
    paxStop.passengerLoad = Load;
    paxStop.basePopulation = BasePopulation;
    paxStop.timetableCode = TimetableCode;
    paxStop.neighbors = NeighborIds.Select(neighborId => FindObjectsOfType<PassengerStop>(true).First(stop => stop.identifier == neighborId)).ToArray();

    var ttc = FindObjectOfType<TimetableController>();

    var branches = Branch.Split(':');
    if (branches.Length > 0) {
      for (int i = 0; i < branches.Length; i++) {
        var brName = branches[i];
        var junctionType = TimetableStation.JunctionType.None;
        if (branches.Length > 1) {
          if (i == 0) {
            junctionType = TimetableStation.JunctionType.JunctionStation;
          } else {
            junctionType = TimetableStation.JunctionType.JunctionDuplicate;
          }
        }
        var branch = ttc.branches.FirstOrDefault(branch => branch.name == brName);
        if (branch == null || branch == default) {
          logger.Information("PaxStop {name} requires branch {brName}, creating", name, brName);
          branch = new TimetableBranch
          {
            name = brName,
            stations = [],
          };
          ttc.branches.Add(branch);
        }
        if (!branch.stations.Exists(station => station.code == TimetableCode)) {
          logger.Information("PaxStop {name} does not exists in TimetableCode, creating.", name);
          var station = new TimetableStation
          {
            passengerStop = paxStop,
            code = TimetableCode,
            name = name,
            junctionType = junctionType
          };
          branch.stations.Add(station);
          branch.stations.Sort((TimetableStation a, TimetableStation b) =>
              (a.passengerStop?.transform.GetComponentInParent<Area>().transform.GetSiblingIndex() ?? int.MaxValue) -
              (b.passengerStop?.transform.GetComponentInParent<Area>().transform.GetSiblingIndex() ?? int.MaxValue));
        } else {
          logger.Information("PaxStop {name} exists in TimetableCode, updating.", name);
          var station = branch.stations.First(station => station.code == TimetableCode);
          station.code = TimetableCode;
          station.name = name;
          station.passengerStop = paxStop;
          station.junctionType = junctionType;
        }
      }
    }

    FillDistances();

    if (wasActive && !paxStop.gameObject.activeSelf)
      paxStop.gameObject.SetActive(true);
    PassengerStopCache.Instance[paxStop.identifier] = paxStop;
    }

  private void FillDistances()
  {
    foreach (var item in FindObjectsOfType<PassengerStop>(true)) {
      if (item.identifier == Identifier) {
        continue;
      }

      if (!PassengerStop.TryCalculateMilesBetweenPassengerStops(this.subIdentifier, item.identifier, out PassengerStop.DistanceInfo foobar)) {
        logger.Warning("Could not calculate distance between {a} and {b}", this.subIdentifier, item.identifier);
      } else {
        logger.Information("Distance between {a} and {b} is {distance} miles", this.subIdentifier, item.identifier, foobar.DistanceInMiles);
      }
    }
  }

  public override void Service(IIndustryContext ctx)
  {

  }

  public override void OrderCars(IIndustryContext ctx)
  {

  }
}
