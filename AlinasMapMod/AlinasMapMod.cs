using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AlinasMapMod.Turntable;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Railloader;
using Serilog;
using SimpleGraph.Runtime;
using TelegraphPoles;
using Track;
using UI.Builder;
using UnityEngine;

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
    private void OnMapDidLoad(MapDidLoadEvent @event)
    {
      logger.Debug("OnMapDidLoad()");
      var tpm = UnityEngine.Object.FindObjectOfType<TelegraphPoleManager>();

      logger.Debug("Adjusting telegraph pole positions");
      var g = tpm.GetComponent<SimpleGraph.Runtime.SimpleGraph>();
      Node n;
      n = g.NodeForId(585);
      n.position += new Vector3(0, 2, 0);
      n = g.NodeForId(587);
      n.position += new Vector3(0, 2, 0);
      n = g.NodeForId(591);
      n.position += new Vector3(0, 2, 0);
      n = g.NodeForId(593);
      n.position += new Vector3(0, 2, 0);
      n = g.NodeForId(595);
      n.position += new Vector3(0, 2, 0);
      n = g.NodeForId(603);
      n.position += new Vector3(0, 2, 0);
      n = g.NodeForId(605);
      n.position += new Vector3(0, 2, 0);

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
      if (settings.ProgressionsDumpPath != "" && !hasDumpedProgressions)
      {
        hasDumpedProgressions = true;
        patcher.Dump(settings.ProgressionsDumpPath);
      }
      patcher.Patch();
      objectCache.Rebuild();

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

    IEnumerable<string> IMixintoProvider.GetMixintos(string mixintoIdentifier)
    {
      yield break;
    }
  }
}
