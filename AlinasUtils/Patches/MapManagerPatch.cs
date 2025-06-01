using System;
using HarmonyLib;
using Map.Runtime;

namespace AlinasUtils.Patches
{
  [HarmonyPatch(typeof(MapManager), "NearbyTileDistance", MethodType.Getter)]
  internal static class MapManagerNearbyTileDistance
  {
    public static void Postfix(ref Single __result)
    {
      __result = AlinasUtilsPlugin.Shared.Settings.MaxTileLoadDistance;
    }
  }
}
