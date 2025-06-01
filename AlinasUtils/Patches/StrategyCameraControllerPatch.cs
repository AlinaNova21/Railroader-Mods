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
    var aup = typeof(AlinasUtilsPlugin);
    var settings = typeof(Settings);
    var strategyCameraController = typeof(StrategyCameraController);
    var distanceField = strategyCameraController.GetField("_distance", BindingFlags.Instance | BindingFlags.NonPublic);
    var sharedPropGetMethod = aup.GetProperty("Shared", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).GetMethod;
    var settingsPropGetMethod = aup.GetProperty("Settings", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod;
    var maxCameraDistancePropGetMethod = settings.GetProperty("MaxCameraDistance", BindingFlags.Instance | BindingFlags.Public).GetMethod;

    // Without ILGenerator, the CodeMatcher will not be able to create labels
    var codeMatcher = new CodeMatcher(instructions, generator);
    codeMatcher
      .End()
      .MatchStartBackwards(
        CodeMatch.StoresField(typeof(StrategyCameraController).GetField("_distance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
      )
      .ThrowIfInvalid("Could not find location to insert code")
      .Advance(-2)
      .RemoveInstruction()
      .Insert(
        // Requires call for each property
        //                   Prop   Prop     Prop
        // AlinasUtilsPlugin.Shared.Settings.MaxCameraDistance
        CodeInstruction.Call(aup, sharedPropGetMethod.Name),
        CodeInstruction.Call(aup, settingsPropGetMethod.Name),
        CodeInstruction.Call(settings, maxCameraDistancePropGetMethod.Name),
        CodeInstruction.Call(typeof(Convert), "ToSingle", [typeof(Int32)])
       );
    var logger = Serilog.Log.ForContext<StrategyCameraController>();
    //logger.Debug("Transpiler for StrategyCameraController.UpdateCameraPosition: {0}", codeMatcher.Instructions().ToString());
    //foreach (var instruction in codeMatcher.Instructions())
    //{
    //  logger.Information("Instruction: {0}", instruction);
    //}
    return codeMatcher.Instructions();
  }
}
