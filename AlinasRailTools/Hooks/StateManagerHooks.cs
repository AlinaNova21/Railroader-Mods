using System.Reflection;
using AlinasRailTools.Runtime;
using Game.State;
using HarmonyLib;
using UnityEngine;

namespace AlinasRailTools.Hooks;

[HarmonyPatch(typeof(StateManager), "OnEnable")]
static class StateManagerHooks
{
  [HarmonyPostfix]
  static void OnEnable_Postfix()
  {
    var gameObject = new GameObject("AlinasRailTools");
    gameObject.AddComponent<ARTManager>();
    gameObject.transform.parent = StateManager.Shared.transform.parent;
  }
}

