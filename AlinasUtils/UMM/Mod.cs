using System.Reflection;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Serilog;
using UnityModManagerNet;

namespace AlinasUtils.UMM;

#if PRIVATETESTING
[EnableReloading]
#endif
internal class Mod
{
  private static Serilog.ILogger Logger = Log.ForContext<Mod>();
  private static readonly Toolbox toolbox = new Toolbox();
  public static bool Loaded { get; private set; } = false;
  public static bool LoadedExternal { get; set; } = false;

  public static bool Load(UnityModManager.ModEntry modEntry)
  {
    if (Loaded || LoadedExternal) {
      Logger.Information($"Already loaded");
      return true;
    }
    Logger.Information($"Load");
    var harmony = new Harmony(modEntry.Info.Id);
    harmony.PatchAll(Assembly.GetExecutingAssembly());
    Messenger.Default.Register<MapDidLoadEvent>(modEntry, OnMapDidLoad);
    modEntry.OnUnload = Unload;
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
}
