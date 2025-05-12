using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Map.Runtime;
using Model.Ops;
using Model.Ops.Definition;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railloader;
using Serilog;
using StrangeCustoms;
using StrangeCustoms.Tracks;
using TelegraphPoles;
using Track;
using UI.Builder;
using UnityEngine;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace AlinasMapMod
{
  public partial class AlinasMapMod : SingletonPluginBase<AlinasMapMod>, IUpdateHandler, IModTabHandler
  {
    private readonly IModdingContext moddingContext;
    private readonly IModDefinition Definition;
    private IUIHelper uIHelper;
    readonly Serilog.ILogger logger = Log.ForContext<AlinasMapMod>();
    readonly Settings settings;
    private ObjectCache ObjectCache { get; } = new ObjectCache();
    public bool HasDumpedProgressions { get; private set; }

    private readonly Patcher patcher = new();

    public AlinasMapMod(IModdingContext _moddingContext, IModDefinition self, IUIHelper _uIHelper)
    {
      moddingContext = _moddingContext;
      Definition = self;
      uIHelper = _uIHelper;

      settings = moddingContext.LoadSettingsData<Settings>(self.Id) ?? new Settings();
    }

    public IEnumerable<ModMixinto> GetMixintos(string identifier)
    {
      var basedir = moddingContext.ModsBaseDirectory;
      foreach (var mixinto in moddingContext.GetMixintos(identifier))
      {
        var path = Path.GetFullPath(mixinto.Mixinto);
        if (!path.StartsWith(basedir))
        {
          logger.Warning($"Mixinto {mixinto} is not in the mods directory, skipping");
        }
        yield return mixinto;
      }
    }

    public override void OnEnable()
    {
      logger.Information("OnEnable() was called!");
      var harmony = new Harmony("AlinaNova21.AlinasMapMod");
      harmony.PatchCategory("AlinasMapMod");
      Messenger.Default.Register<MapWillUnloadEvent>(this, (e) =>
      {
        VanillaPrefabs.ClearCache();
      });

      Messenger.Default.Register<GraphWillChangeEvent>(this, GraphWillChangeEvent);

      #if DEBUG
      // var cd = new ConflictDetector(moddingContext);
      // cd.CheckForConflicts();
      #endif

      Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
      patcher.OnPatchState += (state) =>
      {
        foreach (var pair in state.Progressions)
        {
          foreach (var sectionPair in pair.Value.Sections)
          {
            var section = sectionPair.Value;
            if (settings.FreeMilestones)
            {
              section.DeliveryPhases.Do(dp => dp.Cost = 0);
            }
            if (settings.DeliveryCarCountMultiplier != 1)
            {
              section.DeliveryPhases.Do(dp => dp.Deliveries.Do(d => d.Count = (int)Math.Round(d.Count * settings.DeliveryCarCountMultiplier)));
            }
            if (!settings.EnableDeliveries)
            {
              section.DeliveryPhases.Do(dp => dp.Deliveries = []);
            }
          }
        }
        foreach (var pair in state.MapFeatures)
        {
          foreach (var tg in pair.Value.TrackGroupsEnableOnUnlock)
          {
            if (tg.Value)
            {
              Graph.Shared.SetGroupEnabled(tg.Key, true);
            }
          }
        }
      };
    }
    public override void OnDisable()
    {
      var harmony = new Harmony("AlinaNova21.AlinasMapMod");
      harmony.UnpatchAll("AlinaNova21.AlinasMapMod");
      Messenger.Default.Unregister(this);
    }
    public void Update()
    {
    }

    private void GraphWillChangeEvent(GraphWillChangeEvent @event)
    {
      //GameObject.FindObjectsOfType<PassengerStop>(true)
      //  .ToList()
      //  .ForEach(ps =>
      //  {
      //    logger.Debug("PassengerStop {id}", ps.name);
      //    var activeSelf = ps.gameObject.activeSelf;
      //    var activeInHierarchy = ps.gameObject.activeInHierarchy;
      //    logger.Debug("activeSelf: {activeSelf}, activeInHierarchy: {activeInHierarchy}", ps.name, activeSelf, activeInHierarchy);
      //    logger.Debug("progressionDisabled: {progressionDisabled}", ps.ProgressionDisabled);
      //    var ind = ps.GetComponentInParent<Industry>();
      //    logger.Debug("Industry {id} {activeSelf} {activeInHierachy} {ProgressionDisabled}", ind.name, ind.gameObject.activeSelf, ind.gameObject.activeInHierarchy, ind.ProgressionDisabled);
      //  });
      return; // TODO: finish this
      var toRemove = new List<string>();
      foreach (var spliney in @event.State.Splineys)
      {
        var handler = spliney.Value.GetValue("Handler").Value<string>();
        if (handler != "AlinaNova21.PaxBuilder")
          continue;
        var id = spliney.Key;
        var spanIds = spliney.Value.GetValue("SpanIds").ToObject<List<string>>();
        var industry = spliney.Value.GetValue("Industry").Value<string>();
        var timetableCode = spliney.Value.GetValue("TimetableCode").Value<string>();
        var basePopulation = spliney.Value.GetValue("BasePopulation").Value<int>();
        var neighborIds = spliney.Value.GetValue("NeighborIds").ToObject<List<string>>();

        SerializedIndustry industryObj;
        foreach (var area in @event.State.Areas)
        {
          if(area.Value.Industries.TryGetValue(industry, out industryObj))
          {
            var industryComponent = industryObj.Components.FirstOrDefault(kv => true);
            break;
          }
        }
        /*
          {
            "handler": "AlinasMapMod.PaxBuilder",
            "spanIds": [], // Spans for loading/unloading
            "industry": "", // Required, see example below
            "timetableCode": "", // Required
            // Reference values: Whittier: 30, Ela: 25, Bryson: 50
            "basePopulation": 40,
            // List of ids of other passenger stations.
            // Unsure of exact impact
            "neighborIds": []
          }

          "barkers": {
            "industries": {
              "barkers-station": {
                "name": "Barkers Station",
                "localPosition": { "x": 0, "y": 0, "z": 0},
                "usesContract": false,
                "components": {
                  "ammBarkersStation": {
                    "name": "Barkers Station",
                    "type": "AlinasMapMod.PaxStationComponent",
                    "timetableCode": "BC",
                    // Reference values: Whittier: 30, Ela: 25, Bryson: 50
                    "basePopulation": 10,
                    "loadId": "passengers",
                    "trackSpans": [ // Spans for loading/unloading
                      "PAN_Test_Mod_00"
                    ],
                    // Future support for custom branches, currently supported is "Main" and "Alarka Branch"
                    "branch": "Main",
                    // List of ids of other passenger stations.
                    // Unsure of exact impact
                    "neighborIds": [],
                    "carTypeFilter": "*",
                    "sharedStorage": true
                  }
                }
              }
            }
          }
        */
      }
    }

    private void OnMapDidLoad(MapDidLoadEvent @event)
    {
      logger.Debug("OnMapDidLoad()");

      var tpm = UnityEngine.Object.FindObjectOfType<TelegraphPoleManager>();
      logger.Debug("Adjusting telegraph pole positions");
      var g = tpm.GetComponent<SimpleGraph.Runtime.SimpleGraph>();

      logger.Debug("Done adjusting telegraph pole positions");
    }
    public void ModTabDidOpen(UIPanelBuilder builder)
    {
      CancellationTokenSource cancellationToken = new CancellationTokenSource();
      var commitChanges = () =>
      {
        cancellationToken.Cancel();
        cancellationToken = new CancellationTokenSource();
        Task.Delay(1000, cancellationToken.Token).ContinueWith((_) =>
        {
          Run();
        });
      };

      logger.Debug("Daytime!");
      builder.AddField("Milestone Deliveries", builder.AddToggle(
          () => settings.EnableDeliveries,
          v =>
          {
            settings.EnableDeliveries = v;
            commitChanges();
          }
      ));

      builder.AddField("Free Milestone Costs", builder.AddToggle(
          () => settings.FreeMilestones,
          v =>
          {
            settings.FreeMilestones = v;
            commitChanges();
          }
      ));
      builder.AddField("Car Count Multiplier", builder.AddSlider(
          () => settings.DeliveryCarCountMultiplier,
          () => String.Format("{0:0}%", settings.DeliveryCarCountMultiplier * 100),
          v =>
          {
            settings.DeliveryCarCountMultiplier = (int)v;
            commitChanges();
          },
          1, 5, true, null
      ));
      builder.AddButton("Rebuild Map", () => {
        MapManager.Instance.RebuildAll();
      });
#if DEBUG
      builder.AddSection("Map Tiles", builder =>
      {
        builder.AddField("Map Name", builder.AddInputField(mapName, v => mapName = v));
        var mods = moddingContext.Mods.Select(m => m.Name).ToList();
        builder.AddField("Map Mod", builder.AddDropdown(mods, mods.IndexOf(mapMod), v =>
        {
          mapMod = mods[v];
        }));
        builder.AddField("Bounds", builder.AddInputField(bounds, v => bounds = v));
        builder.AddButton("Generate Missing Tiles", () =>
        {
          GenerateMissingTiles();
        });
      });
#endif
    }
    string bounds { get; set; } = "53,0,60,5";
    string mapMod { get; set; } = "AlinasSandbox";
    string mapName { get; set; } = "BushnellWhittier";

    public void ModTabDidClose()
    {
      logger.Debug("Nighttime...");
      moddingContext.SaveSettingsData(Definition.Id, settings);
      Run();
    }
    
    internal void Run()
    {
      logger.Information($"Run()");
      if (settings.ProgressionsDumpPath != "" && !HasDumpedProgressions)
      {
        HasDumpedProgressions = true;
        patcher.Dump(settings.ProgressionsDumpPath);
      }
      try {
        patcher.Patch();
      } catch (Exception e) {
        logger.Error(e, "Error while patching progressions");
        var window = uIHelper.CreateWindow("AMMProgErr", 200, 200, UI.Common.Window.Position.Center);
        window.Title = "AlinasMapMod Error";
        uIHelper.PopulateWindow(window, (builder) => {
          builder.AddLabel("Error while patching progressions");
          builder.AddLabel(e.Message);
          builder.AddButton("Close", () => window.CloseWindow());
        });
        window.ShowWindow();
      }
      ObjectCache.Rebuild();

      var alinasMapModGameObject = GameObject.Find("AlinasMapMod");
      if (alinasMapModGameObject == null)
      {
        alinasMapModGameObject = new GameObject("AlinasMapMod");
        alinasMapModGameObject.transform.parent = GameObject.Find("World").transform;
        alinasMapModGameObject.transform.localPosition = new Vector3(0, 0, 0);
      }

      // if (!GameObject.Find("AN_Turntable_Test_00")) {
      //   var obj = TurntableGenerator.DumpTree(GameObject.Find("Roundhouse").transform);
      //   File.WriteAllText("turntable.json", obj.ToString());

      //   var tt1 = TurntableGenerator.Generate("AN_Turntable_Test_00", 31);
      //   tt1.transform.parent = alinasMapModGameObject.transform;
      //   tt1.transform.localPosition = new Vector3(12920, 561, 4660);

      //   var tt2 = TurntableGenerator.Generate("AN_Turntable_Test_01", 1);
      //   tt2.transform.parent = alinasMapModGameObject.transform;
      //   tt2.transform.localPosition = new Vector3(13000, 561, 4600);
      // }
    }

    private async Task GenerateMissingTiles() {
#if DEBUG
      try
      {
        var store = AccessTools.Field(typeof(MapManager), "_store").GetValue(MapManager.Instance) as MapStore;
        var orig = new MapStore();
        var origPath = Path.Combine(Application.streamingAssetsPath, "Maps", mapName);
        orig.Load(origPath);
        var mod = moddingContext.Mods.FirstOrDefault(m => m.Name == mapMod);
        var localPath = Path.Combine(mod.Directory, "Maps", mapName);
        var local = new MapStore();
        local.GetType().GetField("_basePath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(local, localPath);
        local.Origin = orig.Origin;
        local.TileDimension = orig.TileDimension;
        var minX = 53;  //-65; -65,20,53,26
        var minY = 0;   //-43;
        var maxX = 60;  //53;
        var maxY = 5;   //20;
        var parts = bounds
          .Split(',')
          .ToList()
          .Select(int.Parse)
          .ToArray();
        minX = parts[0];
        minY = parts[1];
        maxX = parts[2];
        maxY = parts[3];
        for (int x = minX; x <= maxX; x++)
        {
          for (int y = minY; y <= maxY; y++)
          {
            var path = Path.Combine(localPath, string.Format("tile_{0:000}_{1:000}.data", x, y));
            var tile = new Vector2Int(x, y);
            if (!File.Exists(path))
            {
              logger.Debug("Rebuilding tile {0:000}_{1:000}", x, y);
              await local.RebuildTile(tile);
            }
            var desc1 = AccessTools.Field(typeof(MapStore), "_descriptors").GetValue(local) as Dictionary<Vector2Int, TileDescriptor>;
            if (!desc1.ContainsKey(tile))
              desc1.Add(new Vector2Int(x, y), new TileDescriptor(new Vector2Int(x, y), TileDescriptorStatus.Real));
          }
        }
        //MapManager.Instance.UpdateVisibleTilesForPosition(CameraSelector.shared.CurrentCameraGroundPosition);
        logger.Debug("Done rebuilding tiles");
        LoadMaps(store);
      }catch(Exception e)
      {
        logger.Error(e, "Error while generating missing tiles");
      }
#endif
    }

    private Dictionary<Vector2Int, string> tilepaths = new Dictionary<Vector2Int, string>();

    internal void LoadMaps(MapStore store)
    {
      tilepaths.Clear();
      var mapName = MapManager.Instance.directoryName;
      logger.Information($"Loading modded map tiles for {mapName}");
      var desc = AccessTools.Field(typeof(MapStore), "_descriptors").GetValue(store) as Dictionary<Vector2Int, TileDescriptor>;
      foreach (var mod in moddingContext.Mods)
      {
        var path = Path.Combine(mod.Directory, "Maps", mapName);
        if (Directory.Exists(path))
        {
          var cnt = 0;
          foreach (var file in Directory.GetFiles(path, "*.data"))
          {
            var parts = Path.GetFileNameWithoutExtension(file).Split('_');
            var x = int.Parse(parts[1]);
            var y = int.Parse(parts[2]);
            var position = new Vector2Int(x, y);
            desc[position] = new TileDescriptor(new Vector2Int(x, y), TileDescriptorStatus.Real);
            tilepaths[position] = file;
            cnt++;
          }
          logger.Information("Loaded {cnt} tiles from {mod}", cnt, mod.Name);
        }
      }
    }

    internal string GetMapTile(Vector2Int position)
    {
      if (tilepaths.ContainsKey(position))
      {
        return tilepaths[position];
      }
      return "";
    }
  }


  class Map
  {
    public List<MapTile> Tiles { get; set; }
  }


  public class MapTile
  {
    public int X { get; set; }
    public int Y { get; set; }
  }
}
