using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Helpers;
using Track;
using UnityEngine;

namespace MapEditor.HarmonyPatches
{
  [UsedImplicitly]
  [HarmonyPatch(typeof(TrackSegment), "RebuildBezier")]
  internal static class TrackSegmentRebuildBezier
  {

    private static void Postfix(TrackSegment __instance)
    {
      var sh = __instance.transform.Find("TrackSegmentHelper")?.GetComponent<TrackSegmentHelper>();
      if (sh == null)
      {
        var gameObject = new GameObject("TrackSegmentHelper");
        gameObject.transform.SetParent(__instance.transform);
        sh = gameObject.AddComponent<TrackSegmentHelper>();
      }

      sh.Rebuild();
    }

  }
}
