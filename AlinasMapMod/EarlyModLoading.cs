using GalaSoft.MvvmLight.Messaging;
using HarmonyLib;
using UI.Menu;
using UnityEngine;

namespace AlinasMapMod;

[HarmonyPatchCategory("AlinasMapMod")]
internal static class EarlyModLoading
{
  private static bool _loadingWorld = false;
  private static AsyncOperation _asyncOperation = null;

  [HarmonyPatch(typeof(SceneDescriptor), "LoadAsync")]
  [HarmonyPostfix]
  public static void LoadAsync(SceneDescriptor __instance, AsyncOperation __result)
  {
    if (__instance.ToString() == SceneDescriptor.BushnellWhittier.ToString()) {
      __result.allowSceneActivation = false;
      _loadingWorld = true;
      _asyncOperation = __result;
      return;
    }
  }

  [HarmonyPatch(typeof(PersistentLoader), "ShowProgress")]
  [HarmonyPostfix]
  public static void ShowProgress(int intProgress)
  {
    if (!_loadingWorld) return;
    if (intProgress >= 90) {
      Messenger.Default.Send(new SceneActivationEvent());
      _asyncOperation.allowSceneActivation = true;
      _asyncOperation = null;
      _loadingWorld = false;
    }
  }
}

public struct SceneActivationEvent;
