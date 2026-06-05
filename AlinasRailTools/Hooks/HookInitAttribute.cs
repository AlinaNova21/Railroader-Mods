using System;
using System.Reflection;
using HarmonyLib;

namespace AlinasRailTools.Hooks;

public static class HookInit
{
  private static bool _initialized = false;
  private static Harmony _harmony;

  public static void InitAllHooks()
  {
    if (_initialized) return;
    _initialized = true;

    try
    {
      _harmony = new Harmony("dev.alinanova.railtools");
      _harmony.PatchAll(Assembly.GetExecutingAssembly());
      UnityEngine.Debug.Log("AlinasRailTools: Harmony patches applied successfully");
    }
    catch (Exception e)
    {
      UnityEngine.Debug.LogError($"AlinasRailTools: Failed to apply Harmony patches: {e}");
    }
  }
}