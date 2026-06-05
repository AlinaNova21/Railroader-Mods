using System.Collections.Generic;
using System.Reflection;
using AlinasRailTools.Runtime;
using Game.Progression;
using Game.State;
using HarmonyLib;
using Serilog;
using UI.Builder;
using UI.Menu;
using UnityEngine;

namespace AlinasRailTools.Hooks;

[HarmonyPatch]
static class CustomMapHooks
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(CustomMapHooks));

    private static MethodInfo SelectProgressionId => typeof(NewGameMenu).GetMethod("SelectProgressionId", BindingFlags.Instance | BindingFlags.NonPublic);
    private static FieldInfo SetupId => typeof(NewGameMenu).GetField("_setupId", BindingFlags.Instance | BindingFlags.NonPublic);
    private static FieldInfo ProgressionId => typeof(NewGameMenu).GetField("_progressionId", BindingFlags.Instance | BindingFlags.NonPublic);
    private static FieldInfo GameMode => typeof(NewGameMenu).GetField("_gameMode", BindingFlags.Instance | BindingFlags.NonPublic);

    [HarmonyPatch(typeof(NewGameMenu), "BuildMapSelect")]
    [HarmonyPrefix]
    static bool NewGameMenu_BuildMapSelect_Prefix(NewGameMenu __instance, ref RectTransform __result, UIPanelBuilder builder)
    {
        if (CustomMapManager.Instance == null) return true;

        List<string> list = new List<string> { "BushnellWhittier - East Whittier Start" };
        List<string> progressionIds = new List<string> { "ewh" };
        List<string> setupIds = new List<string> { "ewh-steam" };
        var defs = CustomMapManager.Instance.MapDefinitions;
        foreach (var kv in defs)
        {
            var def = kv.Value;
            list.Add(def.Name);
            progressionIds.Add(def.ProgressionId);
            setupIds.Add(def.Identifier);
        }
        int num = progressionIds.IndexOf((string)ProgressionId.GetValue(__instance));
        if (num < 0 && (GameMode)GameMode.GetValue(__instance) == Game.State.GameMode.Company)
        {
            num = 0;
      SelectProgressionId.Invoke(__instance, [progressionIds[num]]);
      SetupId.SetValue(__instance, setupIds[num]);
      SetSelected(setupIds[num]);
        }
        __result = builder.AddDropdown(list, num, delegate (int i)
        {
            SelectProgressionId.Invoke(__instance, [progressionIds[i]]);
            SetupId.SetValue(__instance, setupIds[i]);
            SetSelected(setupIds[i]);
        });
        return false;
    }

    private static void SetSelected(string id)
    {
        if (CustomMapManager.Instance == null) return;

        var defs = CustomMapManager.Instance.MapDefinitions;
        if (defs.ContainsKey(id))
        {
            CustomMapManager.Instance.Selected = defs[id];
            // TODO: Uncomment when Vector3 serialization is fixed
            // CustomMapManager.Instance.SaveSelectedMapToKVO();
        }
        else
        {
            CustomMapManager.Instance.Selected = null;
        }
    }

    [HarmonyPatch(typeof(ProgressionManager), "Awake")]
    [HarmonyPrefix]
    static void ProgressionManager_Awake_Prefix(ProgressionManager __instance)
    {
        CustomMapManager.Instance?.InitMap();
    }
}
