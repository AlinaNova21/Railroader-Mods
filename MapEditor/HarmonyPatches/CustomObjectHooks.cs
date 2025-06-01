using AlinasMapMod.Turntable;
using HarmonyLib;
using MapEditor.Objects;

namespace MapEditor.HarmonyPatches
{
  [HarmonyPatch(typeof(TurntableComponent), "OnEnable")]
  internal class TurntablePatch : HarmonyPatch
  {
    private static void Postfix(TurntableComponent __instance)
    {
      if (__instance.gameObject.GetComponent<EditableTurntable>() == null) {
        __instance.gameObject.AddComponent<EditableTurntable>();
      }
    }
  }
}
