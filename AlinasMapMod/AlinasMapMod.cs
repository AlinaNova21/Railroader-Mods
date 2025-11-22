using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AlinasMapMod.Caches;
using AlinasMapMod.Definitions;
using AlinasMapMod.Definitions.Converters;
using AlinasMapMod.Map;
using AlinasMapMod.Mods;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Map.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog.Core;
using TelegraphPoles;
using Track;
using UI.Builder;
using UnityEngine;

namespace AlinasMapMod;

public partial class AlinasMapMod : Mods.SingletonModBase<AlinasMapMod>
{
  public static JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings
  {
    ContractResolver = new DefaultContractResolver
    {
      NamingStrategy = new CamelCaseNamingStrategy
      {
        ProcessDictionaryKeys = false
      },
    },
    Converters = [
                    new LoadConverter(),
                    new TrackSpanConverter(),
                    new CarTypeFilterConverter(),
                    new ProgressionIndustryComponentConverter(),
                    new Vector3Converter(),
                ],
    Formatting = Formatting.Indented,
  };
  public static JsonSerializer JsonSerializer => JsonSerializer.CreateDefault(JsonSerializerSettings);
  public static readonly string ModDirectory = Path.Combine("Mods", "AlinasMapMod");
  readonly Settings settings;
  internal Settings Settings => settings;
  public bool HasDumpedProgressions { get; private set; }

  private readonly OldPatcher patcher = new();

  public AlinasMapMod()
  {
    settings = AlinasMapModPlugin.Shared.Settings ?? new Settings();
  }

  public List<Type> GetMods() => Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(ModBase).IsAssignableFrom(t))
    .Where(t => t != GetType() && !t.IsAbstract) // Exclude the current mod class and base classes
    .ToList();

  public List<ModBase> Mods { get; private set; } = [];

  public override void Load()
  {
    base.Load();
    var harmony = new Harmony("AlinasMapMod");
    harmony.PatchAll();
    Messenger.Default.Register<MapWillLoadEvent>(this, _ => Utils.ClearCaches());
    Messenger.Default.Register<MapWillUnloadEvent>(this, _ => Utils.ClearCaches());
    Messenger.Default.Register<MapDidLoadEvent>(this, OnMapDidLoad);
    Messenger.Default.Register<GraphDidRebuildCollections>(this, OnGraphDidRebuildCollections);
    //Messenger.Default.Register<SceneActivationEvent>(this, OnSceneActivation);

    patcher.OnPatchState += PatchState;
    Mods.Clear();
    GetMods().ForEach(t => {
      base.Logger.Debug($"Loading mod {t.Name}");
      try {
        var smb = typeof(SingletonModBase<>).MakeGenericType(t);
        var instanceProperty = smb.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        var instanceMethod = instanceProperty.GetMethod;
        var instance = instanceMethod.Invoke(null, []);
        var m = (ModBase)instance;
      //var m = (ModBase)typeof(SingletonModBase<>)
      //  .MakeGenericType(t)
      //  .GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static)
      //  .GetMethod
      //  .MakeGenericMethod(t)
      //  .Invoke(null, []);
        m.Load();
        Mods.Add(m);
      }catch(Exception e) {
        base.Logger.Error(e, "Error while loading mod {modName}", t.Name);
      }
    });
  }

  private void OnGraphDidRebuildCollections(GraphDidRebuildCollections collections) => Messenger.Default.Send(new CachesNeedRebuildEvent());

  public override void Unload()
  {
    base.Unload();
    Messenger.Default.Unregister(this);
    var harmony = new Harmony("AlinasMapMod");
    harmony.UnpatchAll("AlinasMapMod");

    patcher.OnPatchState -= PatchState;
    Mods.ForEach(m => m.Unload());
    Mods.Clear();
  }

  private void PatchState(PatchState state)
  {
    foreach (var pair in state.Progressions) {
      foreach (var sectionPair in pair.Value.Sections) {
        var section = sectionPair.Value;
        if (settings.FreeMilestones) {
          section.DeliveryPhases.Do(dp => dp.Cost = 0);
        }
        if (settings.DeliveryCarCountMultiplier != 1) {
          section.DeliveryPhases.Do(dp => dp.Deliveries.Do(d => d.Count = (int)Math.Round(d.Count * settings.DeliveryCarCountMultiplier)));
        }
        if (!settings.EnableDeliveries) {
          section.DeliveryPhases.Do(dp => dp.Deliveries = []);
        }
      }
    }
    foreach (var pair in state.MapFeatures) {
      foreach (var tg in pair.Value.TrackGroupsEnableOnUnlock) {
        if (tg.Value) {
          Graph.Shared.SetGroupEnabled(tg.Key, true);
        }
      }
    }
  }

  private void OnMapDidLoad(MapDidLoadEvent @event)
  {
    Logger.Debug("OnMapDidLoad()");

    ConsoleCommands.RegisterCommands();

    var tpm = GameObject.FindObjectOfType<TelegraphPoleManager>();
    Logger.Debug("Adjusting telegraph pole positions");
    var g = tpm.GetComponent<SimpleGraph.Runtime.SimpleGraph>();

    Logger.Debug("Done adjusting telegraph pole positions");

    var world = GameObject.Find("World");
    if (world == null) {
      Logger.Debug("World not found");
      return;
    }
  }

  public void BuildSettingsWindow(UIPanelBuilder builder)
  {
    CancellationTokenSource cancellationToken = new CancellationTokenSource();
    var commitChanges = () => {
      cancellationToken.Cancel();
      cancellationToken = new CancellationTokenSource();
      Task.Delay(1000, cancellationToken.Token).ContinueWith((_) => {
        Run();
      });
    };

    Logger.Debug("Daytime!");
    builder.AddField("Milestone Deliveries", builder.AddToggle(
        () => settings.EnableDeliveries,
        v => {
          settings.EnableDeliveries = v;
          commitChanges();
        }
    ));

    builder.AddField("Free Milestone Costs", builder.AddToggle(
        () => settings.FreeMilestones,
        v => {
          settings.FreeMilestones = v;
          commitChanges();
        }
    ));
    builder.AddField("Car Count Multiplier", builder.AddSlider(
        () => settings.DeliveryCarCountMultiplier,
        () => String.Format("{0:0}%", settings.DeliveryCarCountMultiplier * 100),
        v => {
          settings.DeliveryCarCountMultiplier = (int)v;
          commitChanges();
        },
        1, 5, true, null
    ));
    builder.AddButton("Rebuild Map", () => {
      MapManager.Instance.RebuildAll();
    });
    builder.AddField("Download Missing Map Tiles", builder.AddToggle(
        () => settings.DownloadMissingTiles,
        v => {
            settings.DownloadMissingTiles = v;
            TileManager.AllowDownloadingTiles = v;
            commitChanges();
        }
    ));
#if PRIVATETESTING
    builder.AddButton("Rerun Patcher", () =>
    {
      var patcher = new Patcher.GamePatcher();
      Logger.Information("Rerunning patcher");
      patcher.RunPatcher();
      patcher.Dump("Mods/AlinasMapMod/game-dump-patched.xml");
      Logger.Information("Done rerunning patcher");
    });
    builder.AddSection("Map Tiles", builder =>
    {
      builder.AddField("Map Name", builder.AddInputField(MapName, v => MapName = v));
      var mods = Directory.GetDirectories("Mods").Select(d => Path.GetFileName(d)).ToList();
      builder.AddField("Map Mod", builder.AddDropdown(mods, mods.IndexOf(MapMod), v =>
      {
        MapMod = mods[v];
      }));
      builder.AddField("Bounds", builder.AddInputField(Bounds, v => Bounds = v));
      builder.AddButton("Generate Missing Tiles", () =>
      {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        GenerateMissingTiles();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
      });
    });
#endif
    }
  private string Bounds { get; set; } = "53,0,60,5";
  private string MapMod { get; set; } = "AlinasSandbox";
  private string MapName { get; set; } = "BushnellWhittier";

  public void ModTabDidClose()
  {
    Logger.Debug("Nighttime...");
    AlinasMapModPlugin.Shared?.moddingContext.SaveSettingsData("AlinaNova21.AlinasMapMod", settings);
    Run();
  }

  internal void Run()
  {
    Logger.Information($"Run()");
    try {
        Messenger.Default.Send(new CachesNeedRebuildEvent());
        patcher.Patch();
    } catch (Exception e) {
      Logger.Error(e, "Error while patching progressions");
      // TODO: Redo this
      //var window = uIHelper.CreateWindow("AMMProgErr", 200, 200, UI.Common.Window.Position.Center);
      //window.Title = "AlinasMapMod Error";
      //uIHelper.PopulateWindow(window, (builder) =>
      //{
      //  builder.AddLabel("Error while patching progressions");
      //  builder.AddLabel(e.Message);
      //  builder.AddButton("Close", () => window.CloseWindow());
      //});
      //window.ShowWindow();
    }

        //Messenger.Default.Send(new CachesNeedRebuildEvent());

#if PRIVATETESTING
        //var gp = new Patcher.GamePatcher();
        //var dumpFile1 = "Mods/AlinasMapMod/game-dump-orig.xml";
        //var dumpFile2 = "Mods/AlinasMapMod/game-dump-patched.xml";
        //gp.Dump(dumpFile1);
        //Logger.Information("Dumped game state to {file}", dumpFile1);
        //gp.RunPatcher();
        //gp.Dump(dumpFile2);
        //Logger.Information("Dumped game state to {file}", dumpFile2);
#endif
    }
}
