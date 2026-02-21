using System;
using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Caches;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Progression;
using HarmonyLib;
using Helpers;
using JetBrains.Annotations;
using Model.Ops;
using Model.Ops.Definition;
using Model.Ops.Timetable;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using StrangeCustoms.Tracks.Industries;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace AlinasMapMod.Stations;

public class PaxStationComponent : IndustryComponent, ICustomIndustryComponent, IIndustryTrackDisplayable, IProgressionDisablable
{
  private readonly ILogger logger = Log.ForContext(typeof(PaxStationComponent));
  public string TimetableCode { get; set; } = "";
  public int BasePopulation { get; set; } = 40;
  public string[] NeighborIds { get; set; } = [];
  [CanBeNull] public string Branch { get; set; }
  public List<BranchDefinition> BranchDefinitions { get; set; } = new();
  public Load Load { get; set; }

  public void OnEnable()
  {
    Messenger.Default.Register<GraphDidRebuildCollections>(this, UpdatePax);
    logger.Information("PaxStationComponent {name} OnEnable", name);
  }

  public void OnDisable() => Messenger.Default.Unregister<GraphDidRebuildCollections>(this, UpdatePax);

  public void DeserializeComponent(SerializedComponent serializedComponent, PatchingContext ctx)
  {
    serializedComponent.ExtraData.TryGetValue("basePopulation", out JToken basePopulation);
    if (basePopulation != null) BasePopulation = (int)basePopulation;

    serializedComponent.ExtraData.TryGetValue("timetableCode", out JToken timetableCode);
    if (timetableCode != null) TimetableCode = (string)timetableCode;

    serializedComponent.ExtraData.TryGetValue("neighborIds", out JToken rawNeighborIds);
    if (rawNeighborIds != null) NeighborIds = rawNeighborIds.Values<string>().ToArray();

    serializedComponent.ExtraData.TryGetValue("branch", out JToken branch);
    if (branch != null) Branch = (string)branch;
    else if (serializedComponent.ExtraData.TryGetValue("branches", out JToken branches)) {
      List<BranchDefinition> branchDefinitions = branches.ToObject<List<BranchDefinition>>();
      if (branchDefinitions != null) BranchDefinitions = branchDefinitions;
    }

    serializedComponent.ExtraData.TryGetValue("loadId", out JToken passengerLoad);
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
    if (Branch != null) serializedComponent.ExtraData["branch"] = Branch;
    if (BranchDefinitions.Count > 0) serializedComponent.ExtraData["branches"] = JObject.FromObject(BranchDefinitions);
    serializedComponent.ExtraData["loadId"] = Load.id;
  }

  public override bool IsVisible => false;

  private bool Validate()
  {
    PassengerStop paxStop = transform.parent.GetComponentInChildren<PassengerStop>(true);
    if (paxStop != null && paxStop.identifier != subIdentifier) {
      string msg =
        $"Existing station ({paxStop.identifier}) found for PaxStationComponent {subIdentifier}, but ids do not match, ({subIdentifier} != {paxStop.identifier})";
      DisplayError(msg);
      return false;
    }

    if (paxStop != null && paxStop.timetableCode != TimetableCode) {
      string msg =
        $"Existing station (${paxStop.identifier})) found but timetable codes do not match, ({TimetableCode} != {paxStop.timetableCode})";
      DisplayError(msg);
      return false;
    }

    PassengerStop[] areaPaxStops = transform.parent.parent.GetComponentsInChildren<PassengerStop>(true);
    PassengerStop matchingStop = areaPaxStops.SingleOrDefault(p => p.identifier == subIdentifier);
    if (matchingStop != null) {
      Industry myIndustry = GetComponentInParent<Industry>(true);
      Industry stopIndustry = matchingStop.GetComponentInParent<Industry>(true);
      if (myIndustry != stopIndustry) {
        string msg =
          $"Existing station {matchingStop.identifier} found for PaxStationComponent {subIdentifier}, but in another industry ({stopIndustry.identifier}). ({myIndustry.identifier} != {stopIndustry.identifier})";
        DisplayError(msg);
        return false;
      }
    }

    return true;
  }

  private void DisplayError(string msg)
  {
    logger.Error(msg);
    UI.Console.Console console = UI.Console.Console.shared;
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

    PassengerStop paxStop = transform.parent.GetComponentInChildren<PassengerStop>();
    bool wasActive = paxStop != null && paxStop.gameObject.activeSelf;
    if (paxStop == null) {
      logger.Information("PaxStop {name} does not exist, creating", name);
      var go = new GameObject(name);
      wasActive = isActiveAndEnabled;
      go.SetActive(false);
      go.transform.parent = transform.parent;
      paxStop = go.AddComponent<PassengerStop>();
    } else {
      logger.Information("PaxStop {name} already exists active: {wasActive}, updating", paxStop.name, wasActive);
      if (paxStop.identifier != subIdentifier)
        throw new InvalidOperationException(
          $"PaxStationComponent id does not match existing passenger stop, ({subIdentifier} != {paxStop.identifier})");
    }

    paxStop.gameObject.SetActive(false);
    if (TrackSpans.Any())
      paxStop.transform.DestroyAllChildren();
    paxStop.identifier = subIdentifier;
    paxStop.passengerLoad = Load;
    paxStop.basePopulation = BasePopulation;
    paxStop.timetableCode = TimetableCode;
    paxStop.ProgressionDisabled = ProgressionDisabled;
    paxStop.neighbors = FindObjectsOfType<PassengerStop>(true).Where(stop => NeighborIds.Contains(stop.timetableCode)).ToArray();

    SetupTimetable(paxStop);

    if (wasActive && !paxStop.gameObject.activeSelf)
      paxStop.gameObject.SetActive(true);
    PassengerStopCache.Instance[paxStop.identifier] = paxStop;

    AccessTools.StaticFieldRefAccess<PassengerStop, PassengerStop[]>("_allPassengerStops") =
      FindObjectsOfType<PassengerStop>();
  }

  private void SetupTimetable(PassengerStop paxStop)
  {
    TimetableController ttc = TimetableController.Shared;
    if (ttc == null) return;

    List<BranchDefinition> branchDefinitions = BranchDefinitions;
    if (Branch != null) {
      foreach (string branchName in Branch.Split(':')) {
        if (branchDefinitions.Any(b => b.Branch == branchName)) continue;
        branchDefinitions.Add(new BranchDefinition { Branch = branchName, TraverseTimeToNext = 0 });
      }
    }
    
    if (branchDefinitions.Count == 0) 
      branchDefinitions.Add(new BranchDefinition { Branch = "Main", TraverseTimeToNext = 0 });

    for (int i = 0; i < branchDefinitions.Count; i++) {
      BranchDefinition branchDefinition = branchDefinitions[i];
      TimetableStation.JunctionType junctionType = TimetableStation.JunctionType.None;
      if (branchDefinitions.Count > 1) {
        if (i == 0)
          junctionType = TimetableStation.JunctionType.JunctionStation;
        else
          junctionType = TimetableStation.JunctionType.JunctionDuplicate;
      }

      TimetableBranch branch = ttc.branches.FirstOrDefault(branch => branch.name == branchDefinition.Branch);
      if (branch == null || branch == default) {
        logger.Information("PaxStop {name} requires branch {brName}, creating", name, branchDefinition.Branch);
        branch = new TimetableBranch { name = branchDefinition.Branch, stations = [] };
        ttc.branches.Add(branch);
      }

      if (!branch.stations.Exists(station => station.code == TimetableCode)) {
        logger.Information("PaxStop {name} does not exists in TimetableCode, creating.", name.Replace(" Station", "").Replace(" Depot", ""));
        var station = new TimetableStation
        {
          passengerStop = paxStop,
          code = TimetableCode,
          name = paxStop.TimetableName,
          junctionType = junctionType,
          traverseTimeToNext = branchDefinition.TraverseTimeToNext,
          mapFeature = GetMapFeature(branchDefinition.MapFeature)
        };
        var stations = new List<TimetableStation> { station };
        foreach (var betweenStation in branchDefinition.Intermediates)
        {
          stations.Add(new TimetableStation{
            passengerStop = null,
            code = betweenStation.Value.Code,
            name = betweenStation.Key,
            junctionType = TimetableStation.JunctionType.None,
            traverseTimeToNext = betweenStation.Value.TraverseTimeToNext,
            mapFeature = GetMapFeature(branchDefinition.MapFeature)
          });
        }
        
        var newIndex = branch.stations.FindIndex(s =>
          (s.passengerStop?.transform.GetComponentInParent<Area>()?.transform.GetSiblingIndex() ?? int.MaxValue) >
          (paxStop.transform.GetComponentInParent<Area>()?.transform.GetSiblingIndex() ?? int.MaxValue));
        if (newIndex == -1)
          branch.stations.AddRange(stations);
        else
          branch.stations.InsertRange(newIndex, stations);
      } else {
        logger.Information("PaxStop {name} exists in TimetableCode, updating.", name);
        TimetableStation station = branch.stations.First(station => station.code == TimetableCode);
        station.code = TimetableCode;
        station.name = paxStop.TimetableName;
        station.passengerStop = paxStop;
        station.junctionType = junctionType;
        if (branchDefinition.TraverseTimeToNext != 0) station.traverseTimeToNext = branchDefinition.TraverseTimeToNext;
        if (branchDefinition.MapFeature != null)
          station.mapFeature = GetMapFeature(branchDefinition.MapFeature);
      }
    }
  }
  
  

  private static MapFeature GetMapFeature(string identifier)
  {
    if (string.IsNullOrEmpty(identifier)) return null;
    MapFeatureManager manager = MapFeatureManager.Shared;
    if (manager == null) return null;
    return manager.AvailableFeatures.FirstOrDefault(s => s.identifier == identifier);
  }

  public override void Service(IIndustryContext ctx)
  {
  }

  public override void OrderCars(IIndustryContext ctx)
  {
  }

  public class IntermediateStation
  {
    public string Code { get; set; } = "";
    public int TraverseTimeToNext { get; set; } = 0;
  }

  public class BranchDefinition
  {
    public string Branch { get; set; }
    public int TraverseTimeToNext { get; set; }
    public string MapFeature { get; set; } = null;

    public Dictionary<string, IntermediateStation> Intermediates { get; set; } = new();
  }
}