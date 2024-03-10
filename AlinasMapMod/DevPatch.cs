# if DEBUG
using System;
using Game.State;
using HarmonyLib;
using UI.Menu;

namespace AlinasMapMod
{

    [HarmonyPatch(typeof(MenuManager))]
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
      StartGameSinglePlayer(__instance, new GameSetup("mapTest"));
    }
  }
}
# endif