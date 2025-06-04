using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Model.Ops;
using UI.Builder;
using UI.CarInspector;

namespace AlinasMapMod.Patches;

[HarmonyPatch(typeof(CarInspector), "PopulatePassengerCarPanel", [typeof(UIPanelBuilder)])]
[HarmonyPatchCategory("AlinasMapMod")]
internal static class CarInspectorPopulatePassengerCarPanel
{
  static List<PassengerStop> PatchList(List<PassengerStop> ordered)
  {
    var stops = PassengerStop.FindAll().Where(ps => !ps.ProgressionDisabled).ToList();
    var has = new HashSet<PassengerStop>(ordered);
    foreach (var stop in stops) {
      if (!has.Contains(stop)) {
        ordered.Add(stop);
      }
    }
    return ordered;
  }
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
  {
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
