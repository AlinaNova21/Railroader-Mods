using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Helpers;
using Track;
using UnityEngine;

namespace MapEditor.HarmonyPatches
{
  [UsedImplicitly]
  [HarmonyPatch(typeof(TrackNode), "Awake")]
  internal static class TrackNodeAwake
  {

    private static void Postfix(TrackNode __instance)
    {
      if (__instance.GetComponentInChildren<TrackNodeHelper>() == null)
      {
        var gameObject = new GameObject("TrackNodeHelper");
        gameObject.transform.SetParent(__instance.transform);
        gameObject.AddComponent<TrackNodeHelper>();
      }
    }

  }
}
