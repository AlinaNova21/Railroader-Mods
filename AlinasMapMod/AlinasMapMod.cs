using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlinasMapMod.Definitions;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Progression;
using HarmonyLib;
using Railloader;
using Serilog;
using Track;
using UI.Builder;

namespace AlinasMapMod
{
  public partial class AlinasMapMod : SingletonPluginBase<AlinasMapMod>, IUpdateHandler, IModTabHandler, IMixintoProvider
  {
    private IModdingContext moddingContext;
    private IModDefinition definition;
    private IUIHelper uIHelper;
    Serilog.ILogger logger = Log.ForContext<AlinasMapMod>();
    Settings settings;
    private ObjectCache objectCache { get; } = new ObjectCache();
        public bool hasDumpedProgressions { get; private set; }

        private Patcher patcher = new Patcher();

    public AlinasMapMod(IModdingContext _moddingContext, IModDefinition self, IUIHelper _uIHelper)
    {
      moddingContext = _moddingContext;
      definition = self;
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

      Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
      patcher.OnPatchState += (state) => {
        foreach(var pair in state.Progressions)
        {
          foreach(var sectionPair in pair.Value.Sections) {
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
        foreach(var pair in state.MapFeatures) {
          foreach(var tg in pair.Value.TrackGroupsAvailableOnUnlock) {
            if (tg.Value) {
              Graph.Shared.SetGroupAvailable(tg.Key, true);
            }
          }
          foreach(var tg in pair.Value.TrackGroupsEnableOnUnlock) {
            if (tg.Value) {
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
    private void OnMapDidLoad(MapDidLoadEvent @event)
    {
      logger.Debug("OnMapDidLoad()");
      // try {
      //   var groups = new HashSet<string>();
      //   foreach(var seg in UnityEngine.Object.FindObjectsByType<TrackSegment>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) {
      //     if (seg.groupId != null && seg.groupId.StartsWith("SAN_")) {
      //       groups.Add(seg.groupId);
      //     };
      //   }
      //   foreach(var g in groups) {
      //     Graph.Shared.SetGroupEnabled(g, false);
      //     Graph.Shared.SetGroupAvailable(g, false);
      //   }
      //   Graph.Shared.RebuildCollections();
      // } catch (Exception e) {
      //   logger.Error(e, "Error in OnMapDidLoad()");
      // }
    }
    public void ModTabDidOpen(UIPanelBuilder builder)
    {
      CancellationTokenSource cancellationToken = new CancellationTokenSource();
      var commitChanges = () => {
        cancellationToken.Cancel();
        cancellationToken = new CancellationTokenSource();
        Task.Delay(1000, cancellationToken.Token).ContinueWith((_) => {
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
    }
    public void ModTabDidClose()
    {
      logger.Debug("Nighttime...");
      moddingContext.SaveSettingsData(definition.Id, settings);
      Run();
    }

    internal void Run()
    {
      logger.Information($"Run()");
      if (settings.ProgressionsDumpPath != "" && !hasDumpedProgressions) {
        hasDumpedProgressions = true;
        patcher.Dump(settings.ProgressionsDumpPath);
      }
      patcher.Patch();
      objectCache.Rebuild();
    }

    IEnumerable<string> IMixintoProvider.GetMixintos(string mixintoIdentifier)
    {
      yield break;
    }
  }
}
