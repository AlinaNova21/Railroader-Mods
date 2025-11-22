using System.Collections.Generic;
using System.IO;
using AlinasMapMod.Map;
using GalaSoft.MvvmLight.Messaging;
using Railloader;
using Serilog;
using StrangeCustoms;
using UI.Builder;

namespace AlinasMapMod;

public partial class AlinasMapModPlugin : SingletonPluginBase<AlinasMapModPlugin>, IModTabHandler
{
  internal readonly IModdingContext moddingContext;
  internal readonly IModDefinition Definition;
  readonly Serilog.ILogger logger = Log.ForContext<AlinasMapMod>();
  readonly Settings settings;
  internal Settings Settings => settings;

  public AlinasMapModPlugin(IModdingContext _moddingContext, IModDefinition self)
  {
    moddingContext = _moddingContext;
    Definition = self;

    settings = moddingContext.LoadSettingsData<Settings>(self.Id) ?? new Settings();
    TileManager.AllowDownloadingTiles = settings.DownloadMissingTiles;
  }

  public IEnumerable<ModMixinto> GetMixintos(string identifier)
  {
    var basedir = moddingContext.ModsBaseDirectory;
    foreach (var mixinto in moddingContext.GetMixintos(identifier)) {
      var path = Path.GetFullPath(mixinto.Mixinto);
      if (!path.StartsWith(basedir)) {
        logger.Warning($"Mixinto {mixinto} is not in the mods directory, skipping");
      }
      yield return mixinto;
    }
  }

  public override void OnEnable()
  {
    AlinasMapMod.Instance.Load();
    Messenger.Default.Register<GraphWillChangeEvent>(this, OnGraphWillChange);
  }

  private void OnGraphWillChange(GraphWillChangeEvent @event)
  {
    var handlerMigrations = new Dictionary<string, string>
    {
      { "AlinasMapMod.LoaderBuilder", "AlinasMapMod.Loaders.LoaderBuilder" },
      { "AlinasMapMod.MapLabelBuilder", "AlinasMapMod.Map.MapLabelBuilder" },
      { "AlinasMapMod.StationAgentBuilder", "AlinasMapMod.Stations.StationAgentBuilder" },
      { "AlinasMapMod.TelegraphPoleBuilder", "AlinasMapMod.TelegraphPoles.TelegraphPoleBuilder" },
      { "AlinasMapMod.TelegraphPoleMover", "AlinasMapMod.TelegraphPoles.TelegraphPoleMover" },
    };

    var icMigrations = new Dictionary<string, string>
    {
      { "AlinasMapMod.PaxStationComponent", "AlinasMapMod.Stations.PaxStationComponent" },
    };

    foreach (var skv in @event.State.Splineys) {
      var id = skv.Key;
      var raw = skv.Value;
      if (raw == null) continue;
      var handler = raw.GetValue("handler", System.StringComparison.OrdinalIgnoreCase).ToString();
      if (handlerMigrations.TryGetValue(handler, out var newHandler)) {
        logger.Debug($"Migrating {id} from {handler} to {newHandler}");
        raw["handler"] = newHandler;
        @event.MarkChanged("splineys", id);
      }
    }
    foreach (var akv in @event.State.Areas) {
      var areaId = akv.Key;
      var area = akv.Value;
      if (area == null) continue;
      foreach (var ikv in area.Industries) {
        var industryId = ikv.Key;
        var industry = ikv.Value;
        if (industry == null) continue;
        foreach (var kv in industry.Components) {
          var componentId = kv.Key;
          var component = kv.Value;
          if (component == null) continue;
          if (icMigrations.TryGetValue(component.Type, out var newHandler)) {
            logger.Debug($"Migrating {componentId} from {component.Type} to {newHandler}");
            component.Type = newHandler;
            @event.MarkChanged("areas", areaId, "industries", industryId, "components", componentId);
          }
        }
      }
    }
  }

  public override void OnDisable()
  {
    AlinasMapMod.Instance.Unload();
  }

  public void ModTabDidOpen(UIPanelBuilder builder)
  {
    AlinasMapMod.Instance.BuildSettingsWindow(builder);
  }

  public void ModTabDidClose()
  {
    logger.Debug("Nighttime...");
    moddingContext.SaveSettingsData(Definition.Id, AlinasMapMod.Instance.Settings);
  }
}
