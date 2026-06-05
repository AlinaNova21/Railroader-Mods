using System;
using System.Collections.Generic;
using System.Linq;
using AlinasRailTools.Scripting.Graph;
using AlinasRailTools.Scripting.Ops;
using AlinasRailTools.Scripting.Rivers;
using AlinasRailTools.Scripting.Turntables;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers;
using Map.Runtime.MaskComponents;
using UnityEngine;

namespace AlinasRailTools.Scripting;
public class IDGenerator(string prefix)
{
  private int _counter = 0;
  public string Next() => $"{prefix}_{_counter++:D2}";
}

public class IDGenerators(string prefix)
{
  readonly public IDGenerator Node = new IDGenerator($"N{prefix}");
  readonly public IDGenerator Segment = new IDGenerator($"S{prefix}");
  readonly public IDGenerator Span = new IDGenerator($"P{prefix}");
}

public class Helper {
  public IDGenerators IDs { get; }
  public GraphBuilder Graph { get; }
  public OpsBuilder Ops { get; }
  public PrefabBuilder Prefabs { get; }
  public SceneryBuilder Scenery { get; }
  public LoaderBuilder Loaders { get; }
  public TurntableBuilder Turntables { get; }
  public RoundhouseBuilder Roundhouses { get; }
  public RiversBuilder Rivers { get; }
  public string Prefix { get; }

  public static Dictionary<string, SplineProfile> SplineProfiles { get; } = new();

  public Helper(string prefix)
  {
    Prefix = prefix;
    IDs = new(prefix);
    Graph = new(this);
    Ops = new(this);
    Prefabs = new(this);
    Scenery = new(this);
    Loaders = new(this);
    Turntables = new(this);
    Roundhouses = new(this);
    Rivers = new(this);
  }

  static Helper()
  {
    ReloadCaches();
    // Register for map load events to clear caches when map changes
    //Messenger.Default.Register<MapDidLoadEvent>(typeof(Helper), e => ReloadCaches());
  }

  public static void ReloadCaches() {
    GraphBuilder.ReloadCaches();
    OpsBuilder.ReloadCaches();
    PrefabBuilder.ReloadCaches();
    PrefabBuilder.LoadVanillaLoaders(); // Load vanilla loader templates
    PrefabBuilder.LoadVanillaTurntables(); // Load vanilla turntable templates
    PrefabBuilder.LoadVanillaRoundhouses(); // Load vanilla roundhouse templates
    SceneryBuilder.ReloadCaches();
    LoaderBuilder.ReloadCaches();
    TurntableBuilder.ReloadCaches();
    RoundhouseBuilder.ReloadCaches();
    RiversBuilder.ReloadCaches();

    // Load SplineProfiles from existing RiverBuilders
    SplineProfiles.Clear();
    var riverBuilders = UnityEngine.Object
      .FindObjectsByType<RiverBuilder>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    foreach (var rb in riverBuilders)
    {
      // Use reflection to get private splineProfile field
      var profileField = typeof(RiverBuilder).GetField("splineProfile",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (profileField != null)
      {
        var profile = profileField.GetValue(rb) as SplineProfile;
        if (profile != null && !SplineProfiles.ContainsKey(profile.name))
        {
          SplineProfiles[profile.name] = profile;
        }
      }
    }
  }
}
