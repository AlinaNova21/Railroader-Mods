using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using UnityEngine;
using static UI.Common.Window;

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
