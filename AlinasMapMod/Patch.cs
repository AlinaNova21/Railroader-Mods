using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Game.Messages;
using Game.Messages.OpsSnapshot;
using Game.Persistence;
using Game.Progression;
using HarmonyLib;
using Model.Ops;
using Railloader;
using Serilog;
using TelegraphPoles;
using Track;
using UI.Builder;
using UI.CarInspector;

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
  [HarmonyPatch(typeof(CarInspector), "PopulatePassengerCarPanel", [typeof(UIPanelBuilder)])]
  [HarmonyPatchCategory("AlinasMapMod")]
  internal static class CarInspectorPopulatePassengerCarPanel
  {
    static List<PassengerStop> PatchList(List<PassengerStop> ordered)
    {
      var stops = PassengerStop.FindAll().Where(ps => !ps.ProgressionDisabled).ToList();
      var has = new HashSet<PassengerStop>(ordered);
      foreach(var stop in stops)
      {
        if(!has.Contains(stop))
        {
          ordered.Add(stop);
        }
      }
      return ordered;
    }
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
      // Without ILGenerator, the CodeMatcher will not be able to create labels
      var codeMatcher = new CodeMatcher(instructions, generator);
      codeMatcher
        .MatchStartForward(
          CodeMatch.Calls(() => default(UIPanelBuilder).VScrollView(default, default))
        )
        .ThrowIfInvalid("Could not find location to insert code")
        .MatchStartBackwards(
          CodeMatch.LoadsArgument(true, "builder")
        )
        .ThrowIfInvalid("Could not find location to insert code")
        .Advance(-1)
        .Insert([
            CodeInstruction.Call(() => PatchList(default)),
        ]);
      return codeMatcher.Instructions();
    }
  }
}
