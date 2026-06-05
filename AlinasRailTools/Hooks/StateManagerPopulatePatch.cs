using System.Collections.Generic;
using AlinasRailTools.Runtime;
using Game.Messages;
using Game.State;
using HarmonyLib;
using KeyValue.Runtime;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;

namespace AlinasRailTools.Hooks;

/// <summary>
/// Patch StateManager.PopulateFromRemoteSnapshot to populate _art properties early
/// and apply mod state through ManagerRegistry
/// </summary>
[HarmonyPatch(typeof(StateManager), "PopulateFromRemoteSnapshot")]
static class StateManagerPopulatePatch
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(StateManagerPopulatePatch));

    [HarmonyPrefix]
    internal static void PopulateFromRemoteSnapshot_Prefix(Snapshot snapshot)
    {
        if (ARTManager.Shared == null)
        {
            Logger.Debug("ARTManager.Shared is null, skipping early _art population");
            return;
        }

        // Initialize GameObject managers (Ops and Graph should exist by now)
        ARTManager.Shared.InitializeGameObjectManagers();

        if (snapshot.Properties.TryGetValue("_art", out var artProperties))
        {
            Logger.Information("Found _art in snapshot with {Count} properties", artProperties.Count);

            if (StateManager.IsHost)
            {
                // Host: Remove _art from snapshot - we already have it locally
                snapshot.Properties.Remove("_art");
            }
            else
            {
                // Client: Populate our KV object early before StateManager's main loop
                ARTManager.Shared.KeyValueObject.ResetData(
                    PropertyValueConverter.SnapshotToRuntime(artProperties),
                    SetValueOrigin.Remote);
            }
        }

        // Host: Load and merge mods now that managers are initialized
        if (StateManager.IsHost)
        {
            Logger.Information("Host: Loading and merging mods");
            ARTManager.Shared.LoadAndMergeMods();
        }

        // Apply merged state from KeyValue through ManagerRegistry
        // This works for both host (from LoadAndMergeMods) and clients (from snapshot)
        Logger.Information("Applying mod state through ManagerRegistry");
        ARTManager.Shared.ApplyStateFromKeyValue();
    }
}
