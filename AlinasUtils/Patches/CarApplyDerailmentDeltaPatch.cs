using HarmonyLib;
using Model;

namespace AlinasUtils.Patches;

[HarmonyPatch(typeof(Car), "ApplyDerailmentDelta")]
public static class CarApplyDerailmentDeltaPatch
{
  public static bool Prefix(ref float delta)
  {
    var plugin = AlinasUtilsPlugin.Shared;
    if (plugin.IsEnabled && plugin.Settings.DisableDerailing) {
      return false;
    }
    return true;
  }
}
