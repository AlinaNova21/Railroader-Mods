using Game.Messages;
using Game.Messages.OpsSnapshot;
using Game.Persistence;
using Game.Progression;
using HarmonyLib;
using Railloader;
using Serilog;
using Track;

namespace AlinasMapMod
{
  [HarmonyPatch()]
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

  [HarmonyPatch(typeof(Graph), "RebuildCollections")]
  [HarmonyPatchCategory("AlinasMapMod")]
  internal static class GraphRebuildCollections
  {
    private static void Prefix()
    {
      Log.ForContext(typeof(AlinasMapMod)).Debug("GraphRebuildCollections PreFix()");
      // SingletonPluginBase<AlinasMapMod>.Shared?.FixSegments();
    }
  }
}