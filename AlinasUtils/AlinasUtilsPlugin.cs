using System;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Persistence;
using HarmonyLib;
using Railloader;
using Serilog;
using UI.Builder;

namespace AlinasUtils;
public class AlinasUtilsPlugin : SingletonPluginBase<AlinasUtilsPlugin>, IUpdateHandler, IModTabHandler
{
  internal IModdingContext ModdingContext { get; private set; }
  internal IModDefinition Definition { get; private set; }
  internal IUIHelper UIHelper { get; private set; }

  ILogger Logger = Log.ForContext<AlinasUtilsPlugin>();
  private Settings _settings;

  private readonly Toolbox toolbox = new Toolbox();
  internal Settings Settings
  {
    get
    {
      if (_settings == null)
      {
        _settings = ModdingContext.LoadSettingsData<Settings>(Definition.Id) ?? new Settings();
      }
      return _settings;
    }
  }

  public AlinasUtilsPlugin(IModdingContext _moddingContext, IModDefinition self, IUIHelper _uIHelper)
  {
    ModdingContext = _moddingContext;
    Definition = self;
    UIHelper = _uIHelper;
  }

  public void ModTabDidClose()
  {
    ModdingContext.SaveSettingsData(Definition.Id, Settings);
  }

  public void ModTabDidOpen(UIPanelBuilder builder)
  {
    builder.AddField("Disable Derailing", builder.AddToggle(
        () => Settings.DisableDerailing,
        (value) => Settings.DisableDerailing = value
    ));

    builder.AddField("Disable Damage (Does not prevent wear damage)", builder.AddToggle(
        () => Settings.DisableDamage,
        (value) => Settings.DisableDamage = value
    ));

    builder.AddField("Auto Load Save on Startup", builder.AddToggle(
        () => Settings.AutoLoadSaveOnStartup,
        (value) =>
        {
          Settings.AutoLoadSaveOnStartup = value;
          builder.Rebuild();
        }
    ));
    if (Settings.AutoLoadSaveOnStartup)
    {
      var saves = WorldStore.FindSaveInfos().Select(save => save.Name).ToList();
      var currentSave = saves.IndexOf(Settings.SaveToLoadOnStartup);
      builder.AddField("Save to Load on Startup", builder.AddDropdown(
          saves,
          currentSave,
          (index) => Settings.SaveToLoadOnStartup = saves[index]
      ));
    }
  }

  public void Update()
  {
  }

  public override void OnEnable()
  {
    Logger.Information("OnEnable() was called!");
    var harmony = new Harmony(Definition.Id);
    harmony.PatchAll();
    Messenger.Default.Register<MapDidLoadEvent>(this, OnMapDidLoad);
  }

  private void OnMapDidLoad(MapDidLoadEvent @event)
  {
    toolbox.Init();
  }

  public override void OnDisable()
  {
    Logger.Information("OnDisable() was called!");
    var harmony = new Harmony(Definition.Id);
    harmony.UnpatchAll();
    Messenger.Default.Unregister(this);
  }
}
