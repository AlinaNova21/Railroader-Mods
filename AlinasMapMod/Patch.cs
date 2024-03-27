using System.Collections.Generic;
using Game.Messages;
using Game.Messages.OpsSnapshot;
using Game.Persistence;
using Game.Progression;
using HarmonyLib;
using Railloader;
using Serilog;
using TelegraphPoles;
using Track;

namespace AlinasMapMod
{
  [HarmonyPatch()]
  [HarmonyPatch(typeof(MapFeatureManager), "Awake")]
  [HarmonyPatchCategory("AlinasMapMod")]
  internal static class MapFeatureManagerAdd
  {
    private static void Prefix()
    {
      Log.ForContext(typeof(AlinasMapMod)).Debug("MapFeatureManager Awake()");
      SingletonPluginBase<AlinasMapMod>.Shared?.Run();
    }
  }

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

  [HarmonyPatch(typeof(TelegraphPoleManager), "OnEnable")]
  [HarmonyPatchCategory("AlinasMapMod")]
  internal static class TelegraphPoleManagerOnEnable
  {
    private static void Prefix(TelegraphPoleManager __instance)
    {
      Log.ForContext(typeof(AlinasMapMod)).Debug("TelegraphPoleManager OnEnable()");
      var prefabField = typeof(TelegraphPoleManager).GetField("polePrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      var prefabs = (List<TelegraphPole>)prefabField.GetValue(__instance);
      foreach(var pole in prefabs)
      {
        var go = pole.gameObject;
        if(go.GetComponent<TelegraphPoleHelper>() == null)
        {
          go.AddComponent<TelegraphPoleHelper>();
        }
      }
    }
  }
}