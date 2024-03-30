
using HarmonyLib;
using Track;
using UnityEngine;

namespace MapEditor
{

  [HarmonyPatch(typeof(TrackNode), "Awake")]
  internal static class TrackNodeAwake
  {
    private static void Prefix(TrackNode __instance)
    {
      if (!__instance.GetComponent<TrackNodeHelper>())
      {
        __instance.gameObject.AddComponent<TrackNodeHelper>();
      }
    }
  }

  [HarmonyPatch(typeof(TrackSegment), "Awake")]
  internal static class TrackSegmentAwake
  {
    private static void Postfix(TrackSegment __instance)
    {
      // Log.ForContext(typeof(TrackSegmentAwake)).Debug("TrackSegmentAwake PostFix()");
      if (!__instance.GetComponent<SegmentHelper>())
      {
        __instance.gameObject.AddComponent<SegmentHelper>();
      }
    }
  }

  [HarmonyPatch(typeof(TrackSegment), "RebuildBezier")]
  internal static class TrackSegmentRebuildBezier
  {
    private static void Postfix(TrackSegment __instance)
    {
      // Log.ForContext(typeof(TrackSegmentAwake)).Debug("TrackSegmentAwake PostFix()");
      var sh = __instance.transform.Find("SegmentHelper")?.GetComponent<SegmentHelper>();
      if (sh == null)
      {
        var go = new GameObject("SegmentHelper");
        go.transform.SetParent(__instance.transform);
        sh = go.AddComponent<SegmentHelper>();
      }
      sh.Rebuild();
    }
  }

}
