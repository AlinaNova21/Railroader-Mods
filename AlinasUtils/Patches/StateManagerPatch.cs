using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Model.Ops;
using Serilog;
using UnityEngine;

namespace AlinasUtils.Patches;

/*
 * private void WaitTime(float hours)
    {
        if (!StateManager.IsHost)
        {
            return;
        }
        base.StartCoroutine(this.WaitTimeCoroutine(hours));
    }
    */


[HarmonyPatch(typeof (StateManager), "WaitTime")]
internal class StateManagerWaitTimePatch
{
    static private IEnumerator WaitTimeCoroutine(float hours)
    {
        var sm = StateManager.Shared;
        float timeMultiplier = 1f; // Force time multiplier to 1 for waiting
        float remaining = hours * 60f * 60f;
        Log.Debug<float, float>("WaitTime {hours} -> {dt}", hours, remaining);
        GameDateTime timeCursor = TimeWeather.Now;
        while (remaining > 0f) {
            float num = Mathf.Min(3600f, remaining);
            Industry.TickAll(num / timeMultiplier);
            timeCursor = timeCursor.AddingSeconds(num);
            StateManager.ApplyLocal(new SetTimeOfDay((float)timeCursor.TotalSeconds));
            remaining -= num;
            yield return new WaitForSeconds(hours > 1 ? 0.1f : 0.25f);
        }
        Log.Debug<float>("WaitTime {hours} complete", hours);
        yield break;
    }

    internal static bool Prefix(float hours, StateManager __instance)
    {
        if (StateManager.IsHost)
        {
            __instance.StartCoroutine(WaitTimeCoroutine(hours));
        }
        return false; // Call original method
    }
}