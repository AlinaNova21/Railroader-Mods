using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AlinasMapMod.Definitions;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Progression;
using HarmonyLib;
using Model.OpsNew;
using Railloader;
using Serilog;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace AlinasMapMod
{
  public partial class AlinasMapMod : SingletonPluginBase<AlinasMapMod>, IUpdateHandler, IModTabHandler
  {
    private IModdingContext moddingContext;
    private IModDefinition definition;
    private IUIHelper uIHelper;
    Serilog.ILogger logger = Log.ForContext<AlinasMapMod>();
    Settings settings;
    Dictionary<string, MapModDef> MixinCache = new Dictionary<string, MapModDef>();
    Vector3 TrackingNodePos = new Vector3();
    float carCountMultiplier = 1;

    // This SC version supports industries, so we don't need to
    // create them unless on an older version
    Version industryMinVersion = new Version(1, 6, 24073, 1827);
    Version scRecommendedVersion = new Version(1, 6, 24074, 728);
    Version scRequiredVersion = new Version(1, 6, 24069, 2251);
    Version scVersion { get => Version.Parse(moddingContext.Mods.Single(m => m.Id == "Zamu.StrangeCustoms")?.Version ?? ""); }
    bool scOutdated { get => scRequiredVersion > scVersion; }

    DateTime nextTry = DateTime.MaxValue;
    HashSet<string> WaitingOnTrackSpans = new HashSet<string>();
    HashSet<string> WaitingOnIndustries = new HashSet<string>();
    HashSet<string> WaitingOnIndustryComponents = new HashSet<string>();

    private ObjectCache objectCache { get; } = new ObjectCache();

    public AlinasMapMod(IModdingContext _moddingContext, IModDefinition self, IUIHelper _uIHelper)
    {
      moddingContext = _moddingContext;
      definition = self;
      uIHelper = _uIHelper;

      if (scOutdated)
      {
        logger.Error($"Available Strange Customs is {scVersion}, which is older than the required {scRequiredVersion}");
      }

      if (scVersion < scRecommendedVersion)
      {
        logger.Warning($"Available Strange Customs is {scVersion}, which is older than the recommended minimum of {scRecommendedVersion}");
      }

      settings = moddingContext.LoadSettingsData<Settings>(self.Id) ?? new Settings();
    }

    void Rebuild()
    {
      carCountMultiplier = 1;
      MixinCache.Clear();
      Run();
    }

    public IEnumerable<ModMixinto> GetMixintos(string identifier) {
      var basedir = moddingContext.ModsBaseDirectory;
      foreach(var mixinto in moddingContext.GetMixintos(identifier)) {
        var path = Path.GetFullPath(mixinto.Mixinto);
        if (!path.StartsWith(basedir)) {
          logger.Warning($"Mixinto {mixinto} is not in the mods directory, skipping");
        }
        yield return mixinto;
      }
    }

    MapModDef GetMapModDef(string file)
    {
      if (MixinCache.ContainsKey(file))
      {
        return MixinCache[file];
      }
      var obj = MapModDef.Parse(file);
      MixinCache.Add(file, obj);
      return obj;
    }

    public override void OnEnable()
    {
      logger.Information("OnEnable() was called!");
      var harmony = new Harmony("AlinaNova21.AlinasMapMod");
      harmony.PatchCategory("AlinasMapMod");

      Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
    }
    public override void OnDisable()
    {
      var harmony = new Harmony("AlinaNova21.AlinasMapMod");
      harmony.UnpatchAll("AlinaNova21.AlinasMapMod");
      Messenger.Default.Unregister(this);
    }
    public void Update()
    {
      var nodes = UnityEngine.Object.FindObjectsByType<TrackNode>(FindObjectsInactive.Include, FindObjectsSortMode.None)
          .ToDictionary(n => n.id);

      if (nextTry < DateTime.Now)
      {
        nextTry = DateTime.Now.AddMilliseconds(500);
        if (nodes.ContainsKey("AlinasMapMod"))
        {
          var node = nodes["AlinasMapMod"];
          if (node.transform.position != TrackingNodePos)
          {
            logger.Information($"Tracking node moved {TrackingNodePos}=>{node.transform.position}");
            Graph.Shared?.RebuildCollections();
            TrackingNodePos = node.transform.position;
          }
        }
        objectCache.Rebuild();
        var run = false;

        if (WaitingOnIndustries.Count > 0)
        {
          logger.Debug($"Waiting on Industries: {String.Join(",", WaitingOnIndustries.ToArray())}");
        }
        if (WaitingOnIndustryComponents.Count > 0)
        {
          logger.Debug($"Waiting on IndustryComponents: {String.Join(",", WaitingOnIndustryComponents.ToArray())}");
        }
        if (WaitingOnTrackSpans.Count > 0)
        {
          logger.Debug($"Waiting on TrackSpans: {String.Join(",", WaitingOnTrackSpans.ToArray())}");
        }

        run |= LoopAndRemove(WaitingOnIndustries);
        run |= LoopAndRemove(WaitingOnIndustryComponents);
        run |= LoopAndRemove(WaitingOnTrackSpans);

        if (run) Run();
      }
    }
    private void OnMapDidLoad(MapDidLoadEvent @event)
    {
      logger.Information("OnMapDidLoad()");
      if (scOutdated)
      {
        var window = uIHelper.CreateWindow(600, 200, Window.Position.Center);
        window.Title = "Alina's Map Mod";
        uIHelper.PopulateWindow(window, pb =>
        {
          pb.AddLabel($"<color=#ff0000>ERROR: Available Strange Customs is {scVersion}, which is older than the required {scRequiredVersion}.", (_) => { });
          pb.AddLabel("This mod will be disabled while running under an old version.");
        });
        window.ShowWindow();
      }

      if (scVersion < scRecommendedVersion)
      {
        var window = uIHelper.CreateWindow(600, 200, Window.Position.Center);
        window.Title = "Alina's Map Mod";
        uIHelper.PopulateWindow(window, pb =>
        {
          pb.AddLabel($"<color=#FFFF00>WARNING: Available Strange Customs is {scVersion}, which is older than the recommended minimum of {scRecommendedVersion}.", (_) => { });
          pb.AddLabel("Newer versions of SC contain more features and bugfixies, please consider upgrading SC to prevent issues in the future.");
        });
        window.ShowWindow();
      }

      var sections = UnityEngine.Object.FindObjectsByType<Section>(FindObjectsSortMode.None);
      foreach(var section in sections) {
        logger.Information($"Section: {section.identifier} {section.name}");
      }
    }
    public void ModTabDidOpen(UIPanelBuilder builder)
    {
      logger.Debug("Daytime!");
      if (scOutdated)
      {
        builder.AddLabel($"<color=#ff0000>ERROR: Available Strange Customs is {scVersion}, which is older than the required {scRequiredVersion}.", (_) => { });
        return;
      }
      builder.AddField("Milestone Deliveries", builder.AddToggle(
          () => settings.EnableDeliveries,
          v =>
          {
            settings.EnableDeliveries = v;
            Rebuild();
          }
      ));

      builder.AddField("Free Milestone Costs", builder.AddToggle(
          () => settings.FreeMilestones,
          v =>
          {
            settings.FreeMilestones = v;
            Rebuild();
          }
      ));
      builder.AddField("Car Count Multiplier", builder.AddSlider(
          () => settings.DeliveryCarCountMultiplier,
          () => String.Format("{0:0}%", settings.DeliveryCarCountMultiplier * 100),
          v =>
          {
            settings.DeliveryCarCountMultiplier = v;
            Rebuild();
          },
          1, 5, false, null
      ));

      builder.AddSection("Map Sections", builder =>
      {
        var mixins = moddingContext.GetMixintos("AlinasMapMod");
        foreach (var mixin in mixins)
        {
          builder.AddSection(mixin.Source.Name, builder =>
          {
            var obj = GetMapModDef(mixin.Mixinto);
            foreach (var item in obj.Items.Values)
            {
              builder.AddLabel(item.Name);
            }
          });
        }
      });
    }
    public void ModTabDidClose()
    {
      logger.Debug("Nighttime...");
      moddingContext.SaveSettingsData(definition.Id, settings);
      Rebuild();
    }

    internal void Run()
    {
      if (scOutdated) return;
      if (nextTry == DateTime.MaxValue) nextTry = DateTime.Now.AddMilliseconds(500);
      logger.Information($"Run()");

      var patcher = new Patcher();
      patcher.Dump();
      patcher.Patch();
      objectCache.Rebuild();
      var mixins = moddingContext.GetMixintos("AlinasMapMod");

      foreach (var mixin in mixins)
      {
        logger.Debug($"Processing mixinto for mod {mixin.Source} {mixin.Mixinto}");
        var obj = GetMapModDef(mixin.Mixinto);
        foreach (var item in obj.Items.Values)
        {
          logger.Information($"Processing Item {item.Identifier} - {item.Name}");
          // ConfigureFeature(item);
          if (scVersion < industryMinVersion)
          {
            logger.Information($"Older SC ({scVersion} < {industryMinVersion}), Managing Industries manually");
            if (item.TrackSpans.Length > 0)
            {
              ConfigureIndustry(item);
              ConfigureProgressionIndustryComponent(item);
            }
          }
          // ConfigureSection(item);
        }
      }
    }
    private void ConfigureIndustry(MapModItem item)
    {
      logger.Debug($"Configuring Industry for {item.Identifier}");
      Industry industry;
      if (objectCache.Industries.ContainsKey(item.Identifier))
      {
        industry = objectCache.Industries[item.Identifier];
      }
      else
      {
        logger.Information($"Industry for {item.Identifier} does not exist, creating");
        var area = UnityEngine.Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(a => a.identifier == item.Area);

        var obj = new GameObject();
        obj.SetActive(false);
        industry = obj.AddComponent<Industry>();
        industry.identifier = item.Identifier;
        objectCache.Industries.Add(item.Identifier, industry);
        obj.transform.parent = area.transform;
        obj.SetActive(true);
      }
      industry.name = item.Name;
      industry.usesContract = false;
    }
    private void ConfigureProgressionIndustryComponent(MapModItem item)
    {
      logger.Debug($"Configuring ProgressionIndustryComponent for {item.Identifier}");
      var id = item.IndustryComponent ?? $"{item.Identifier}.site";
      var subId = id.Split('.')[1];
      ProgressionIndustryComponent component;
      if (objectCache.IndustryComponents.ContainsKey(id))
      {
        component = (ProgressionIndustryComponent)objectCache.IndustryComponents[id];
      }
      else
      {
        logger.Information($"ProgressionIndustryComponent {id} does not exist, creating");
        var area = UnityEngine.Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(a => a.identifier == item.Area);

        var obj = new GameObject();
        obj.SetActive(false);
        component = obj.AddComponent<ProgressionIndustryComponent>();
        component.subIdentifier = subId;
        component.gameObject.SetActive(false);
        component.carTypeFilter = new CarTypeFilter("*");
        component.name = $"{item.Name} Site";
        component.ProgressionDisabled = false;
        component.sharedStorage = true;
        objectCache.IndustryComponents.Add(id, component);
        obj.transform.parent = objectCache.Industries[item.Identifier].transform;
      }

      if ((component.trackSpans == null || component.trackSpans.Length == 0) && item.TrackSpans.Length > 0)
      {
        var spans = UnityEngine.Object.FindObjectsByType<TrackSpan>(FindObjectsInactive.Include, FindObjectsSortMode.None)
          .Where(ts => item.TrackSpans.Contains(ts.id))
          .ToArray();
        var foundSpanIds = spans.Select(s => s.id);
        logger.Information($"Updating trackspans on industry {id} Wanted: ({string.Join(",", item.TrackSpans)}) Found: ({string.Join(",", foundSpanIds)})");
        foreach (var span in item.TrackSpans)
        {
          if (!foundSpanIds.Contains(span))
          {
            WaitingOnTrackSpans.Add(span);
          }
        }
        component.trackSpans = spans;
        component.gameObject.SetActive(true);
      }
      item.IndustryComponent = component.Identifier;
    }
    private void ConfigureFeature(MapModItem item)
    {
      logger.Debug($"Configuring MapFeature for {item.Identifier}");
      MapFeature feat;
      var featManager = UnityEngine.Object.FindAnyObjectByType<MapFeatureManager>(FindObjectsInactive.Include);
      if (objectCache.MapFeatures.ContainsKey(item.Identifier))
      {
        feat = objectCache.MapFeatures[item.Identifier];
      }
      else
      {
        logger.Information($"MapFeature for {item.Identifier} does not exist, creating");
        GameObject obj = new GameObject();
        obj.SetActive(false);
        feat = obj.AddComponent<MapFeature>();
        feat.identifier = item.Identifier;
        objectCache.MapFeatures.Add(item.Identifier, feat);
        obj.transform.parent = featManager.transform;
        obj.SetActive(true);

        var graph = UnityEngine.Object.FindAnyObjectByType<Graph>(FindObjectsInactive.Include);
        foreach (var id in item.GroupIds)
        {
          graph.SetGroupEnabled(id, true);
        }
      }
      feat.displayName = item.Name;
      feat.name = item.Name;
      feat.trackGroupsEnableOnUnlock = item.GroupIds;
      feat.trackGroupsAvailableOnUnlock = item.GroupIds;
      feat.unlockExcludeIndustries = [];
      feat.unlockIncludeIndustries = [];
      feat.unlockIncludeIndustryComponents = [];
      feat.areasEnableOnUnlock = [];
      feat.gameObjectsEnableOnUnlock = [];
    }
    private void ConfigureSection(MapModItem item)
    {
      logger.Debug($"Configuring section for {item.Identifier}");
      var progression = UnityEngine.Object.FindAnyObjectByType<Progression>(FindObjectsInactive.Include);
      Section sec;
      if (objectCache.Sections.ContainsKey(item.Identifier))
      {
        sec = objectCache.Sections[item.Identifier];
      }
      else
      {
        logger.Information($"Section for {item.Identifier} does not exist, creating");
        GameObject obj = new GameObject();
        obj.SetActive(false);
        sec = obj.AddComponent<Section>();
        sec.identifier = item.Identifier;
        objectCache.Sections.Add(item.Identifier, sec);
        obj.transform.parent = progression?.transform;
        obj.SetActive(true);
      }
      sec.name = item.Name;
      sec.deliveryPhases = item.DeliveryPhases;
      sec.disableFeaturesOnUnlock = [];
      sec.displayName = item.Name;
      sec.enableFeaturesOnAvailable = [];
      sec.enableFeaturesOnUnlock = [objectCache.MapFeatures[item.Identifier]];
      sec.prerequisiteSections = item.PrerequisiteSections.Select(s => objectCache.Sections[s]).ToArray();
      sec.description = item.Description;

      var reload = false;
      var hasIc = objectCache.IndustryComponents.ContainsKey(item.IndustryComponent);
      if (!hasIc)
      {
        sec.gameObject.SetActive(false);
        WaitingOnIndustryComponents.Add(item.IndustryComponent);
      }
      else
      {
        var ic = objectCache.IndustryComponents[item.IndustryComponent];
        foreach (var dp in item.DeliveryPhases)
        {
          reload |= settings.EnableDeliveries && (dp.industryComponent != ic);
          if (dp.industryComponent != ic)
          {
            sec.gameObject.SetActive(true);
            dp.industryComponent = (ProgressionIndustryComponent)ic;
          }
          dp.cost = settings.FreeMilestones ? 0 : dp.cost;
          var mult = settings.DeliveryCarCountMultiplier / carCountMultiplier;
          reload |= settings.EnableDeliveries == (dp.deliveries.Length == 0);
          if (!settings.EnableDeliveries)
          {
            dp.deliveries = [];
          }
          foreach (var d in dp.deliveries)
          {
            d.count = (int)Math.Round(d.count * mult);
          }
          reload |= mult != 1;
          carCountMultiplier = mult;
        }
        if (reload)
        {
          ic.gameObject.SetActive(false);
          ic.gameObject.SetActive(true);
        }
      }
    }
    private bool LoopAndRemove<T>(HashSet<T> set)
    {
      var toRemove = new List<T>();
      foreach (var id in set)
      {
        if (set.Contains(id))
        {
          toRemove.Add(id);
        }
      }
      toRemove.ForEach(id => set.Remove(id));
      return toRemove.Count > 0;
    }
  }
}
