using System;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Persistence;
using HarmonyLib;
using Railloader;
using Serilog;
using UI.Builder;

namespace AlinasCore;
public class AlinasCorePlugin : SingletonPluginBase<AlinasCorePlugin>, IUpdateHandler, IModTabHandler
{
  internal IModdingContext ModdingContext { get; private set; }
  internal IModDefinition Definition { get; private set; }
  internal IUIHelper UIHelper { get; private set; }

  ILogger Logger = Log.ForContext<AlinasCorePlugin>();
  private Settings _settings;

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

  public AlinasCorePlugin(IModdingContext _moddingContext, IModDefinition self, IUIHelper _uIHelper)
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
  }

  public override void OnDisable()
  {
    Logger.Information("OnDisable() was called!");
    var harmony = new Harmony(Definition.Id);
    harmony.UnpatchAll();
    Messenger.Default.Unregister(this);
  }
}
