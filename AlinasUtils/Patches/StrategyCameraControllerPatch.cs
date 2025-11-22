using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cameras;
using HarmonyLib;

namespace AlinasUtils.Patches;
[HarmonyPatch(typeof(StrategyCameraController), "UpdateCameraPosition")]
internal static class StrategyCameraControllerUpdateCameraPositionPatch
{

  internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
  {
    // Simplified: just replace the 500f constant with 5000f
    var codeMatcher = new CodeMatcher(instructions, generator);
    codeMatcher
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldc_R4, 1f),
        new CodeMatch(OpCodes.Ldc_R4, 500f)
      )
      .ThrowIfInvalid("Could not find camera distance constants")
      .Advance(1) // Move to the 500f instruction
      .SetOperandAndAdvance(5000f);
    return codeMatcher.Instructions();
  }
}
