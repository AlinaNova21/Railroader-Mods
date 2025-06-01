using System.Collections.Generic;
using System.Reflection;
using AlinasMapMod.TelegraphPoles;
using HarmonyLib;
using Serilog;
using TelegraphPoles;

namespace AlinasMapMod.Patches;

[HarmonyPatch(typeof(TelegraphPoleManager), "OnEnable")]
[HarmonyPatchCategory("AlinasMapMod")]
internal static class TelegraphPoleManagerOnEnable
{
  internal static void Prefix(TelegraphPoleManager __instance)
  {
    Log.ForContext(typeof(AlinasMapMod)).Debug("TelegraphPoleManager OnEnable()");
    var prefabField = typeof(TelegraphPoleManager).GetField("polePrefabs", BindingFlags.NonPublic | BindingFlags.Instance);
    var prefabs = (List<TelegraphPole>)prefabField.GetValue(__instance);
    foreach (var pole in prefabs) {
      var go = pole.gameObject;
      if (go.GetComponent<TelegraphPoleHelper>() == null) {
        go.AddComponent<TelegraphPoleHelper>();
      }
    }
  }
}
