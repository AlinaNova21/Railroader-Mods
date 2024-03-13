using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Progression;
using HarmonyLib;
using Model.Database;
using Model.OpsNew;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Railloader;
using Serilog;
using TMPro;
using Track;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace AlinasMapMod
{
    public partial class AlinasMapMod : SingletonPluginBase<AlinasMapMod>, IUpdateHandler, IModTabHandler
    {

        bool initialized = false;
        public IModdingContext moddingContext;
        IModDefinition definition;
        Serilog.ILogger logger = Log.ForContext<AlinasMapMod>();
        Settings settings;

        Dictionary<string, MapFeature> MapFeatures = new Dictionary<string, MapFeature>();
        Dictionary<string, Section> Sections = new Dictionary<string, Section>();
        Dictionary<string, Industry> Industries = new Dictionary<string, Industry>();
        Dictionary<string, IndustryComponent> IndustryComponents = new Dictionary<string, IndustryComponent>();
        Dictionary<string, TrackSpan> TrackSpans = new Dictionary<string, TrackSpan>();

        Dictionary<string, MixinDef> MixinCache = new Dictionary<string, MixinDef>();

        Vector3 TrackingNodePos = new Vector3();

        float carCountMultiplier = 1;

        // This SC version supports industries, so we don't need to
        // create them unless on an older version
        Version industryMinVersion = new Version(1, 6, 24073, 1827);
        
        // Version scRequiredVersion = new Version(1, 6, 99999, 2251);
        Version scRequiredVersion = new Version(1, 6, 24069, 2251);
        Version scVersion
        {
            get => Version.Parse(moddingContext.Mods.Single(m => m.Id == "Zamu.StrangeCustoms")?.Version ?? "");
        }
        bool scOutdated
        {
            get => scRequiredVersion > scVersion;
        }

        DateTime nextTry = DateTime.MaxValue;
        HashSet<string> WaitingOnTrackSpans = new HashSet<string>();
        HashSet<string> WaitingOnIndustries = new HashSet<string>();
        HashSet<string> WaitingOnIndustryComponents = new HashSet<string>();

        void RebuildCache()
        {
            MapFeatures = UnityEngine.Object.FindObjectsByType<MapFeature>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .ToDictionary(v => v.identifier);
            Sections = UnityEngine.Object.FindObjectsByType<Section>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .ToDictionary(v => v.identifier);
            Industries = UnityEngine.Object.FindObjectsByType<Industry>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .ToDictionary(v => v.identifier);
            IndustryComponents = UnityEngine.Object.FindObjectsByType<IndustryComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .ToDictionary(v => v.gameObject.GetComponentInParent<Industry>(true).identifier + "." + v.subIdentifier);
            TrackSpans = UnityEngine.Object.FindObjectsByType<TrackSpan>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .ToDictionary(v => v.id);
        }

        public AlinasMapMod(IModdingContext _moddingContext, IModDefinition self)
        {
            moddingContext = _moddingContext;
            definition = self;

            if (scOutdated)
            {
                logger.Error($"Cannot load AlinasMapMod: Available Strange Customs is {scVersion}, which is older than the required {scRequiredVersion}");
                // throw new InvalidOperationException($"Incompatible Strange Customs detected, version {required} or higher needed for track support.");
            }

            settings = moddingContext.LoadSettingsData<Settings>(self.Id) ?? new Settings();
        }

        public override void OnEnable()
        {
            logger.Information("OnEnable() was called!");
            var harmony = new Harmony("AlinaNova21.AlinasMapMod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Harmony.DEBUG = true;
            var methods = Harmony.GetAllPatchedMethods().ToArray();
            foreach(var method in methods)
            {
                logger.Debug($"Patched method: {method.FullDescription()}");
                var pi = Harmony.GetPatchInfo(method);
                logger.Debug(String.Join(", ", pi.Owners));
            }
            

            Messenger.Default.Register(this, new Action<MapWillLoadEvent>(OnMapWillLoad));
            Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
        }

        private void OnMapDidLoad(MapDidLoadEvent @event)
        {
            logger.Information("OnMapDidLoad()");
            if (scOutdated)
            {
                var wc = UnityEngine.Object.FindAnyObjectByType<ProgrammaticWindowCreator>();
                var window = UnityEngine.Object.Instantiate(wc.windowPrefab, wc.transform, false);
                window.SetContentSize(new Vector2((float)500, (float)100));
                window.SetPosition(Window.Position.Center);
                window.Title = "Alina's Map Mod";

                UIPanel.Create(window.contentRectTransform, wc.builderAssets, pb =>
                {
                    // pb.AddTitle("Alina's Map Mod", "Strange Customs Error");
                    pb.AddLabel($"<color=#ff0000>ERROR: Available Strange Customs is {scVersion}, which is older than the required {scRequiredVersion}.", (_) => { });
                    pb.AddLabel("This mod will be disabled while running under an old version.");
                });
                window.ShowWindow();

            }
            // Run();
        }

        internal void Run()
        {
            if (scOutdated) return;
            if (nextTry == DateTime.MaxValue) nextTry = DateTime.Now.AddMilliseconds(500);
            logger.Information($"Run()");
            RebuildCache();
            var mixins = moddingContext.GetMixintos("AlinasMapMod");

            foreach (var mixin in mixins)
            {
                logger.Debug($"Processing mixinto for mod {mixin.Source} {mixin.Mixinto}");
                var obj = GetMixin(mixin.Mixinto);
                foreach (var item in obj.Items.Values)
                {
                    logger.Information($"Processing Item {item.Identifier} - {item.Name}");
                    ConfigureFeature(item);
                    if (scVersion < industryMinVersion)
                    {
                        logger.Information($"Older SC ({scVersion} < {industryMinVersion}), Managing Industries manually");
                        if (item.TrackSpans.Length > 0)
                        {
                            ConfigureIndustry(item);
                            ConfigureProgressionIndustryComponent(item);
                        }
                    }
                    ConfigureSection(item);
                }
            }
            var ps = PrefabStore.Create();
        }
        private void ConfigureIndustry(MapModItem item)
        {
            logger.Debug($"Configuring Industry for {item.Identifier}");
            Industry industry;
            if (Industries.ContainsKey(item.Identifier))
            {
                industry = Industries[item.Identifier];
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
                Industries.Add(item.Identifier, industry);
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
            if (IndustryComponents.ContainsKey(id))
            {
                component = (ProgressionIndustryComponent)IndustryComponents[id];
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
                IndustryComponents.Add(id, component);
                obj.transform.parent = Industries[item.Identifier].transform;
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
        void ConfigureFeature(MapModItem item)
        {
            logger.Debug($"Configuring MapFeature for {item.Identifier}");
            MapFeature feat;
            var featManager = UnityEngine.Object.FindAnyObjectByType<MapFeatureManager>(FindObjectsInactive.Include);
            if (MapFeatures.ContainsKey(item.Identifier))
            {
                feat = MapFeatures[item.Identifier];
            }
            else
            {
                logger.Information($"MapFeature for {item.Identifier} does not exist, creating");
                GameObject obj = new GameObject();
                obj.SetActive(false);
                feat = obj.AddComponent<MapFeature>();
                feat.identifier = item.Identifier;
                MapFeatures.Add(item.Identifier, feat);
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
            if (Sections.ContainsKey(item.Identifier))
            {
                sec = Sections[item.Identifier];
            }
            else
            {
                logger.Information($"Section for {item.Identifier} does not exist, creating");
                GameObject obj = new GameObject();
                obj.SetActive(false);
                sec = obj.AddComponent<Section>();
                sec.identifier = item.Identifier;
                Sections.Add(item.Identifier, sec);
                obj.transform.parent = progression?.transform;
                obj.SetActive(true);
            }
            sec.name = item.Name;
            sec.deliveryPhases = item.DeliveryPhases;
            sec.disableFeaturesOnUnlock = [];
            sec.displayName = item.Name;
            sec.enableFeaturesOnAvailable = [];
            sec.enableFeaturesOnUnlock = [MapFeatures[item.Identifier]];
            sec.prerequisiteSections = item.PrerequisiteSections.Select(s => Sections[s]).ToArray();
            sec.description = item.Description;

            var reload = false;
            var hasIc = IndustryComponents.ContainsKey(item.IndustryComponent);
            if (!hasIc)
            {
                sec.gameObject.SetActive(false);
                WaitingOnIndustryComponents.Add(item.IndustryComponent);
            }
            else
            {
                var ic = IndustryComponents[item.IndustryComponent];
                foreach (var dp in item.DeliveryPhases)
                {
                    // reload |= settings.EnableDeliveries == (dp.industryComponent == null);
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

        private void OnMapWillLoad(MapWillLoadEvent @event)
        {
            initialized = false;
        }

        public override void OnDisable()
        {
            Messenger.Default.Unregister(this);
        }

        static AlinasMapMod()
        {
        }

        public void Update()
        {
            var nodes = UnityEngine.Object.FindObjectsByType<TrackNode>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .ToDictionary(n => n.id);
            if (nodes.ContainsKey("AlinasMapMod"))
            {
                var node = nodes["AlinasMapMod"];
                if (node.transform.position == TrackingNodePos)
                {
                    logger.Information($"Tracking node moved {TrackingNodePos}=>{node.transform.position}");
                    Graph.Shared?.RebuildCollections();
                    TrackingNodePos = node.transform.position;
                }
            }

            if (nextTry < DateTime.Now)
            {
                nextTry = DateTime.Now.AddMilliseconds(500);
                RebuildCache();
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
                foreach (var id in WaitingOnIndustries)
                {
                    if (Industries.ContainsKey(id))
                    {
                        WaitingOnIndustries.Remove(id);
                        run = true;
                    }
                }
                foreach (var id in WaitingOnIndustryComponents)
                {
                    if (IndustryComponents.ContainsKey(id))
                    {
                        WaitingOnIndustryComponents.Remove(id);
                        run = true;
                    }
                }
                foreach (var id in WaitingOnTrackSpans)
                {
                    if (TrackSpans.ContainsKey(id))
                    {
                        WaitingOnTrackSpans.Remove(id);
                        run = true;
                    }
                }
                if (run) Run();
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

            builder.AddSection("Map Sections");

            var mixins = moddingContext.GetMixintos("AlinasMapMod");

            foreach (var mixin in mixins)
            {
                var obj = GetMixin(mixin.Mixinto);
                foreach (var item in obj.Items.Values)
                {
                    builder.AddLabel(item.Name);
                }
            }
        }

        public void ModTabDidClose()
        {
            logger.Debug("Nighttime...");
            moddingContext.SaveSettingsData(definition.Id, settings);
            Rebuild();
        }

        MixinDef GetMixin(string file)
        {
            if (MixinCache.ContainsKey(file))
            {
                return MixinCache[file];
            }
            var obj = LoadAndParseMixin(file);
            MixinCache.Add(file, obj);
            return obj;
        }

        MixinDef LoadAndParseMixin(string file)
        {
            var jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = false
                    }
                },
                Converters = [
                    new LoadConverter(),
                    new TrackSpanConverter(),
                    new CarTypeFilterConverter(),
                    new ProgressionIndustryComponentConverter(),
                ]
            });

            var obj = JObject.Parse(File.ReadAllText(file)).ToObject<MixinDef>(jsonSerializer);
            if (obj == null)
            {
                logger.Warning($"Obj was null from mixin {file}");
            }
            return obj;
        }

        void Rebuild()
        {
            carCountMultiplier = 1;
            MixinCache.Clear();
            RebuildCache();
            Run();
        }
    }
}
