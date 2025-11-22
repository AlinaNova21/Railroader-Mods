using System.Collections.Generic;
using Game.State;
using HarmonyLib;
using Network;
using UI.Menu;

namespace AlinasUtils.Patches;

[HarmonyPatch(typeof(MenuManager), "Start")]
internal static class MenuManagerStartPatch
{
  private static bool launched;

  [HarmonyPostfix]
  public static void SkipStart(MenuManager __instance)
  {
    var settings = AlinasUtilsPlugin.Shared?.Settings ?? UMM.Mod.Settings;
    if (!settings.AutoLoadSaveOnStartup) return;
    if (launched) return;
    launched = true;
    var gameManagerField = typeof(MenuManager).GetField("gameManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var gameManager = (GlobalGameManager)gameManagerField.GetValue(__instance);

    var descriptor = SceneDescriptor.BushnellWhittier;

    List<SceneDescriptor> list1 = new List<SceneDescriptor>
      {
        #if PRIVATETESTING
        SceneDescriptor.Editor,
        #endif
        descriptor,
        SceneDescriptor.EnvironmentEnviro
      };
    var sceneLoadSetup = new GlobalGameManager.SceneLoadSetup(list1, descriptor);
    var gameSetup = new GameSetup(settings.SaveToLoadOnStartup);
    gameManager.Launch(sceneLoadSetup, gameSetup, default(StartSingleplayerSetup));
  }
}
