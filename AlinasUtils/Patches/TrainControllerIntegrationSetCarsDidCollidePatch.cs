using System;
using HarmonyLib;
using Model;

namespace AlinasUtils.Patches;

[HarmonyPatch(typeof(TrainController), "IntegrationSetCarsDidCollide", [typeof(Car), typeof(Car), typeof(float), typeof(bool)])]
public static class TrainControllerIntegrationSetCarsDidCollidePatch
{
  public static bool Prefix(Car car0, Car car1, float deltaVelocity, bool isIn)
  {
    var plugin = AlinasUtilsPlugin.Shared;
    if (plugin.IsEnabled && plugin.Settings.DisableCollisions)
    {
      return false;
    }
    return true;
  }
}
