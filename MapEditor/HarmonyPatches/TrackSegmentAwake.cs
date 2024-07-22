using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Helpers;
using Track;
using UnityEngine;

namespace MapEditor.HarmonyPatches
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  [HarmonyPatch(typeof(TrackSegment), "Awake")]
  internal static class TrackSegmentAwake
  {

    private static void Postfix(TrackSegment __instance)
    {
      if (__instance.GetComponentInChildren<TrackSegmentHelper>() == null)
      {
        var gameObject = new GameObject("TrackSegmentHelper");
        gameObject.transform.SetParent(__instance.transform);
        var sh = gameObject.AddComponent<TrackSegmentHelper>();
        sh.Rebuild();
      }
    }

  }
}
