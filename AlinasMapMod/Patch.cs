using Game.Progression;
using HarmonyLib;
using Railloader;
using Serilog;

namespace AlinasMapMod
{
  [HarmonyPatch(typeof(MapFeatureManager), "Awake")]
  [HarmonyPatchCategory("AlinasMapMod")]
  internal static class MapFeatureManagerAdd
  {
    private static void Prefix()
    {
      Log.ForContext(typeof(AlinasMapMod)).Debug("MapFeatureManager Awake()");
      SingletonPluginBase<AlinasMapMod>.Shared?.Run();
    }
  }
}