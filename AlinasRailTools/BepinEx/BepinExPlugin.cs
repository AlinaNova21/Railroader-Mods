using AlinasRailTools.Hooks;
using AlinasRailTools.Scripting;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;

namespace AlinasRailTools.BepinEx;

[BepInEx.BepInPlugin("dev.alinanova.railtools", "Alina's Rail Tools", "1.0.0")]
internal class Plugin : BepInEx.BaseUnityPlugin
{
  public static Plugin Shared { get; private set; }
  public Runner ScriptRunner { get; } = new Runner();

  public Plugin() : base() {
    Shared = this;

    // Initialize hooks (idempotent - safe to call even if UMM already called it)
    HookInit.InitAllHooks();

    Messenger.Default.Register<MapDidLoadEvent>(this, _ => {
      ScriptRunner.FindAndRunScripts();
    });
  }

  public void Start()
  {
  }
}
