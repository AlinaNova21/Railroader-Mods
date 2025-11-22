using HarmonyLib;
using Model;

namespace AlinasUtils.Patches;

[HarmonyPatch(typeof(Car), "ApplyDerailmentDelta")]
public static class CarApplyDerailmentDeltaPatch
{
  public static bool Prefix(ref float delta)
  {
    var settings = AlinasUtilsPlugin.Shared?.Settings ?? UMM.Mod.Settings;
    if (settings.DisableDamage && delta < 0) {
      return false;
    }
    return true;
  }
}
