
using HarmonyLib;
using Railloader;
using Serilog;
using Track;

namespace AlinasMapMod.Editor
{

  [HarmonyPatch(typeof(Graph), "RebuildCollections")]
  [HarmonyPatchCategory("AlinasMapMod.Editor")]
  internal static class GraphRebuildCollections
  {
    private static void Prefix()
    {
      Log.ForContext(typeof(GraphRebuildCollections)).Debug("GraphRebuildCollections PostFix()");
      // SingletonPluginBase<AlinasMapMod>.Shared?.AttachGizmos();
    }
  }
}