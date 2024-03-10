
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using Game.Messages.OpsSnapshot;
using Game.Progression;
using Game.State;
using HarmonyLib;
using Model.OpsNew;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Railloader;
using Serilog;
using Serilog.Core;
using Track;
using UI.Menu;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace AlinasMapMod
{

  [HarmonyPatch]
  internal static class MapFeatureManagerAdd
  {
    private static IEnumerable<MethodBase> TargetMethods()
    {
      return [
        AccessTools.Method("Game.Progression.ProgressionManager:Awake", null, null),
        AccessTools.Method("Game.Progression.MapFeatureManager:Awake", null, null),
      ];
    }

    private static void Prefix()
    {
      Log.ForContext(typeof(AlinasMapMod)).Debug("ProgressionManager or MapFeatureManager Awake()");
      SingletonPluginBase<AlinasMapMod>.Shared?.Run();
    }
  }
  [HarmonyPatch(typeof(Graph), "RebuildCollections")]
  internal static class GraphRebuildPatch
  {
    private static void Prefix()
    {
      SingletonPluginBase<AlinasMapMod>.Shared.Run();
    }
  }
}