# if DEBUG
using System;
using System.Collections.Generic;
using Game.State;
using HarmonyLib;
using Network;
using UI.Menu;

namespace AlinasMapMod
{

  [HarmonyPatch(typeof(MenuManager))]
  [HarmonyPatchCategory("AlinasMapMod")]
  internal static class MenuManagerPatches
  {
    private static bool launched;

    [HarmonyReversePatch]
    [HarmonyPatch("StartGameSinglePlayer")]
    private static void StartGameSinglePlayer(MenuManager __instance, GameSetup setup)
        => throw new NotImplementedException();

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    public static void SkipStart(MenuManager __instance)
    {
      if (launched)
        return;
      launched = true;
      // StartGameSinglePlayer(__instance, new GameSetup("mapTest"));
      var gameManagerField = typeof(MenuManager).GetField("gameManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      var gameManager = (GlobalGameManager)gameManagerField.GetValue(__instance);

      // var descriptor = new SceneDescriptor("Assets/Scenes/TestScenes/OpsTest.unity");
      var descriptor = SceneDescriptor.BushnellWhittier;
      
      List<SceneDescriptor> list1= new List<SceneDescriptor>
      {
        // SceneDescriptor.Editor,
        descriptor,
        SceneDescriptor.EnvironmentEnviro
      };
      gameManager.Launch(new GlobalGameManager.SceneLoadSetup(list1, descriptor), null, default(StartSingleplayerSetup));
    }
  }
}
# endif