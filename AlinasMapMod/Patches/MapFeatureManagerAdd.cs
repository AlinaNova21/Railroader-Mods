using Game.Progression;
using HarmonyLib;
using Serilog;

namespace AlinasMapMod.Patches;

[HarmonyPatch()]
[HarmonyPatch(typeof(MapFeatureManager), "Awake")]
[HarmonyPatchCategory("AlinasMapMod")]
internal static class MapFeatureManagerAdd
{
  internal static void Prefix()
  {
    Log.ForContext(typeof(AlinasMapMod)).Debug("MapFeatureManager Awake()");
    AlinasMapMod.Instance?.Run();
  }
}
