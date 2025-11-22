using HarmonyLib;
using Serilog;
using Track;

namespace AlinasMapMod.Patches;

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