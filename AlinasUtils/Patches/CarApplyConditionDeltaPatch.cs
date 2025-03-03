using HarmonyLib;
using Model;
using Model.Physics;

namespace AlinasUtils.Patches;

[HarmonyPatch(typeof(Car), "ApplyConditionDelta", [typeof(float)])]
public static class CarApplyConditionDeltaPatch
{
  public static bool Prefix(float delta)
  {
    var plugin = AlinasUtilsPlugin.Shared;
    if (plugin.IsEnabled && plugin.Settings.DisableDamage && delta < 0)
    {
      return false;
    }
    return true;
  }
}
