using HarmonyLib;
using Model.Physics;

namespace AlinasUtils.Patches;

[HarmonyPatch(typeof(TrainMath), "MaximumSpeedMphForCurve", [typeof(float)])]
public static class TrainMathMaximumSpeedMphForCurvePatch
{
  public static bool Prefix(ref float __result)
  {
    var plugin = AlinasUtilsPlugin.Shared;
    if (plugin.IsEnabled && plugin.Settings.DisableDamageOnCurves)
    {
      __result = 1000f;
      return false;
    }
    return true;
  }
}
