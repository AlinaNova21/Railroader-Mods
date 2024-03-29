
using System;
using HarmonyLib;
using Railloader;
using Serilog;
using Track;
using UnityEngine;

namespace MapEditor
{

  [HarmonyPatch(typeof(Graph), "RebuildCollections")]
  internal static class GraphRebuildCollections
  {
    private static void Prefix()
    {
      Log.ForContext(typeof(GraphRebuildCollections)).Debug("GraphRebuildCollections PostFix()");
      // SingletonPluginBase<AlinasMapMod>.Shared?.AttachGizmos();
      UnityEngine.Object.FindObjectOfType<NodeHelpers>()?.Reset();
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