using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace MapEditor.HarmonyPatches
{
  [HarmonyPatch(typeof(PatchEditor), "AddOrUpdateScenery")]
  internal static class PatchEditorAddOrUpdateScenery
  {

    private static bool Prefix(PatchEditor __instance, string sceneryId, string modelIdentifier, Vector3 position, Vector3 eulerRotation, Vector3 scale)
    {
      var changeThing = __instance.GetType().GetMethod("ChangeThing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      var scAsm = Assembly.GetAssembly(typeof(PatchEditor));
      var graphPatcherType = scAsm.GetType("StrangeCustoms.Tracks.GraphPatcher");
      var serializerProp = graphPatcherType.GetProperty("Serializer", BindingFlags.Static | BindingFlags.NonPublic);
      var serializer = (JsonSerializer)serializerProp.GetGetMethod(true).Invoke(null, null);
      changeThing.Invoke(__instance, ["scenery", sceneryId, JObject.FromObject(new
      {
        ModelIdentifier = modelIdentifier,
        Position = position,
        Rotation = eulerRotation,
        Scale = scale
      }, serializer), true]);
      return false;
    }
  }
}
