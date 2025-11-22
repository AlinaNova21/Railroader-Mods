using System.Reflection;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Serilog;
using UnityModManagerNet;

namespace AlinasUtils.UMM;

public class SettingsWrapper : UnityModManager.ModSettings
{
  public string SaveToLoadOnStartup { get; set; } = "";
  public bool AutoLoadSaveOnStartup { get; set; } = false;
  public bool DisableDerailing { get; set; } = false;
  public bool DisableDamage { get; set; } = false;
  public int MaxCameraDistance { get; set; } = 500;
  public int MaxTileLoadDistance { get; set; } = 1500;

  public Settings ToSettings()
  {
    return new Settings
    {
      SaveToLoadOnStartup = SaveToLoadOnStartup,
      AutoLoadSaveOnStartup = AutoLoadSaveOnStartup,
      DisableDerailing = DisableDerailing,
      DisableDamage = DisableDamage,
      MaxCameraDistance = MaxCameraDistance,
      MaxTileLoadDistance = MaxTileLoadDistance
    };
  }

  public static SettingsWrapper FromSettings(Settings settings)
  {
    return new SettingsWrapper
    {
      SaveToLoadOnStartup = settings.SaveToLoadOnStartup,
      AutoLoadSaveOnStartup = settings.AutoLoadSaveOnStartup,
      DisableDerailing = settings.DisableDerailing,
      DisableDamage = settings.DisableDamage,
      MaxCameraDistance = settings.MaxCameraDistance,
      MaxTileLoadDistance = settings.MaxTileLoadDistance
    };
  }
}

#if PRIVATETESTING
[EnableReloading]
#endif
internal class Mod
{
  private static Serilog.ILogger Logger = Log.ForContext<Mod>();
  private static readonly Toolbox toolbox = new Toolbox();
  public static bool Loaded { get; private set; } = false;
  public static bool LoadedExternal { get; set; } = false;
  public static Settings Settings { get; private set; } = new Settings();
  private static SettingsWrapper settingsWrapper;
  private static UnityModManager.ModEntry modEntry;

  public static bool Load(UnityModManager.ModEntry modEntry)
  {
    if (Loaded || LoadedExternal) {
      Logger.Information($"Already loaded");
      return true;
    }
    Logger.Information($"Load");
    Mod.modEntry = modEntry;
    settingsWrapper = UnityModManager.ModSettings.Load<SettingsWrapper>(modEntry);
    if (settingsWrapper != null) {
      Settings = settingsWrapper.ToSettings();
    } else {
      settingsWrapper = new SettingsWrapper();
      Settings = new Settings();
    }
    var harmony = new Harmony(modEntry.Info.Id);
    harmony.PatchAll(Assembly.GetExecutingAssembly());
    Messenger.Default.Register<MapDidLoadEvent>(modEntry, OnMapDidLoad);
    modEntry.OnUnload = Unload;
    modEntry.OnSaveGUI = OnSaveGUI;
    Loaded = true;
    return true;
  }

  public static bool Unload(UnityModManager.ModEntry modEntry)
  {
    if (!Loaded || LoadedExternal) {
      Logger.Information($"Already unloaded");
      return true;
    }
    Loaded = false;
    Logger.Information($"Unload");
    var harmony = new Harmony(modEntry.Info.Id);
    harmony.UnpatchAll(modEntry.Info.Id);
    Messenger.Default.Unregister(modEntry);
    toolbox.Dispose();
    return true;
  }

  private static void OnMapDidLoad(MapDidLoadEvent @event)
  {
    toolbox.Init();
  }

  private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
  {
    settingsWrapper = SettingsWrapper.FromSettings(Settings);
    UnityModManager.ModSettings.Save(settingsWrapper, modEntry);
  }
}
