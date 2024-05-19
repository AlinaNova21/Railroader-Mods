
using HarmonyLib;
using Serilog;
using Track;
using UnityEngine;

namespace MapEditor
{

  [HarmonyPatch(typeof(TrackNode), "Awake")]
  internal static class TrackNodeAwake
  {
    private static void Postfix(TrackNode __instance)
    {
      if (__instance.GetComponentInChildren<TrackNodeHelper>() == null)
      {
        var go = new GameObject("TrackNodeHelper");
        go.transform.SetParent(__instance.transform);
        go.AddComponent<TrackNodeHelper>();
      }
    }
  }

  [HarmonyPatch(typeof(TrackSegment), "Awake")]
  internal static class TrackSegmentAwake
  {
    private static void Postfix(TrackSegment __instance)
    {
      if (__instance.GetComponentInChildren<SegmentHelper>() == null)
      {
        var go = new GameObject("SegmentHelper");
        go.transform.SetParent(__instance.transform);
        go.AddComponent<SegmentHelper>();
      }
    }
  }

  [HarmonyPatch(typeof(TrackSegment), "RebuildBezier")]
  internal static class TrackSegmentRebuildBezier
  {
    private static void Postfix(TrackSegment __instance)
    {
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
