using HarmonyLib;
using Model;

namespace AlinasUtils.Patches;

[HarmonyPatch(typeof(Car), "ApplyConditionDelta", [typeof(float)])]
public static class CarApplyConditionDeltaPatch
{
  public static bool Prefix(float delta)
  {
    var settings = AlinasUtilsPlugin.Shared?.Settings ?? UMM.Mod.Settings;
    if (settings.DisableDamage && delta < 0) {
      return false;
    }
    return true;
  }
}
