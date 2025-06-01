using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Helpers;
using Track;

namespace MapEditor.HarmonyPatches
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  [HarmonyPatch(typeof(TrackSegment), "RebuildBezier")]
  internal static class TrackSegmentRebuildBezier
  {

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static void Postfix(TrackSegment __instance)
    {
      if (__instance.id == "ghost") return;
      var helper = __instance.transform.Find("TrackSegmentHelper")?.GetComponent<TrackSegmentHelper>();
      if (helper != null) {
        helper.Rebuild();
      }
    }

  }
}
