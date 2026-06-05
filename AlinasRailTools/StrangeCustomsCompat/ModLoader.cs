using System;
using System.Collections.Generic;
using System.IO;
using AlinasRailTools.Runtime;
using AlinasRailTools.Scripting;
using AlinasRailTools.Scripting.Graph;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat;

/// <summary>
/// DEPRECATED: Old builder-based mod loader for StrangeCustoms mods
///
/// This loader directly applies mods using the builder system (GameGraphProcessor).
/// It is being replaced by the new ManagerRegistry-based system which:
/// - Uses StateFile format for better structure
/// - Supports dependency-ordered managers
/// - Allows proper resource references via ResId<T>
/// - Provides better separation of concerns
///
/// The new system flow is:
/// 1. Host loads mods via ARTManager.LoadAndMergeMods()
/// 2. Converts game-graph.json to StateFile via LegacyConverter
/// 3. Merges all StateFiles via StateMerger
/// 4. Syncs single merged StateFile JSON via KeyValue
/// 5. Both host/clients apply via ManagerRegistry.ApplyToAll()
///
/// This class remains for backward compatibility but should not be used for new features.
/// </summary>
[Obsolete("Use ARTManager.LoadAndMergeMods() with ManagerRegistry instead")]
public class ModLoader
{
    private readonly Helper helper;
    private readonly HashSet<string> failedMods = new();

    public ModLoader(Helper helper)
    {
        this.helper = helper;
    }

    /// <summary>
    /// Discover, resolve, and load all mods from the specified directory
    /// </summary>
    public void LoadMods(string modsDirectory)
    {
        Debug.Log("ModLoader: Starting mod discovery and loading process...");

        try
        {
            // Clear state from previous runs
            failedMods.Clear();

            // Discover mods
            var discovery = new ModDiscovery();
            var discoveredMods = discovery.DiscoverMods(modsDirectory);

            if (discoveredMods.Count == 0)
            {
                Debug.Log("ModLoader: No mods discovered");
                return;
            }

            // Resolve dependencies and get load order
            var resolver = new DependencyResolver(discoveredMods);
            var loadOrder = resolver.ResolveLoadOrder();

            // Apply mods in dependency order
            ApplyMods(discoveredMods, loadOrder, null);

            Debug.Log($"ModLoader: Successfully processed {discoveredMods.Count} mods ({failedMods.Count} failed)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ModLoader: Error during mod loading: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Discover, resolve, and load all mods from VFS
    /// </summary>
    public void LoadModsFromVFS(VirtualFileSystem vfs)
    {
        Debug.Log("ModLoader: Starting mod discovery and loading process from VFS...");
        Helper.ReloadCaches();
        try
            {
            // Clear state from previous runs
            failedMods.Clear();

            // Discover mods from VFS
            var discovery = new ModDiscovery();
            var discoveredMods = discovery.DiscoverModsFromVFS(vfs);

            if (discoveredMods.Count == 0)
            {
                Debug.Log("ModLoader: No mods discovered in VFS");
                return;
            }

            // Resolve dependencies and get load order
            var resolver = new DependencyResolver(discoveredMods);
            var loadOrder = resolver.ResolveLoadOrder();

            // Apply mods in dependency order from VFS
            ApplyMods(discoveredMods, loadOrder, vfs);

            Debug.Log($"ModLoader: Successfully processed {discoveredMods.Count} mods from VFS ({failedMods.Count} failed)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ModLoader: Error during VFS mod loading: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Apply mods in dependency order with batch mode
    /// </summary>
    private void ApplyMods(Dictionary<string, ModDefinition> discoveredMods, List<string> loadOrder, VirtualFileSystem vfs)
    {
        Debug.Log("ModLoader: Applying mods in dependency order...");

        // Begin batch mode to prevent enumeration errors during graph updates
        TrackNodeBuilder.BeginBatch();
        try
        {
            foreach (var modId in loadOrder)
            {
                // Skip if dependency failed
                if (failedMods.Contains(modId))
                {
                    Debug.LogWarning($"ModLoader: Skipping mod '{modId}' - dependency failed");
                    continue;
                }

                if (discoveredMods.TryGetValue(modId, out var mod))
                {
                    if (vfs != null)
                    {
                        ApplyModFromVFS(mod, vfs);
                    }
                    else
                    {
                        ApplyMod(mod);
                    }
                }
            }
        }
        finally
        {
            // End batch mode and process all pending invalidations
            TrackNodeBuilder.EndBatch();
        }

        Debug.Log("ModLoader: Mod application complete");
    }

    /// <summary>
    /// Apply a single mod by processing its game-graph files
    /// </summary>
    private void ApplyMod(ModDefinition mod)
    {
        Debug.Log($"ModLoader: Applying mod '{mod.Name}' ({mod.Id})");

        try
        {
            var gameGraphFiles = mod.GetGameGraphFiles();

            if (gameGraphFiles.Count == 0)
            {
                Debug.Log($"ModLoader: Mod '{mod.Name}' has no game-graph files, skipping");
                return;
            }

            var processor = new GameGraphProcessor(helper);

            foreach (var filePath in gameGraphFiles)
            {
                var fullPath = Path.Combine(mod.DirectoryPath, filePath);
                if (File.Exists(fullPath))
                {
                    Debug.Log($"ModLoader: Processing game graph file: {fullPath}");
                    processor.ProcessGameGraph(fullPath);
                }
                else
                {
                    Debug.LogWarning($"ModLoader: Game graph file not found: {fullPath}");
                }
            }

            Debug.Log($"ModLoader: Successfully applied mod '{mod.Name}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ModLoader: Error applying mod '{mod.Name}' ({mod.Id}): {ex.Message}");
            MarkModAsFailed(mod.Id);
        }
    }

    /// <summary>
    /// Apply a single mod from VFS by processing its game-graph files
    /// </summary>
    private void ApplyModFromVFS(ModDefinition mod, VirtualFileSystem vfs)
    {
        Debug.Log($"ModLoader: Applying mod '{mod.Name}' ({mod.Id}) from VFS");

        try
        {
            var gameGraphFiles = mod.GetGameGraphFiles();

            if (gameGraphFiles.Count == 0)
            {
                Debug.Log($"ModLoader: Mod '{mod.Name}' has no game-graph files, skipping");
                return;
            }

            var processor = new GameGraphProcessor(helper);

            foreach (var filePath in gameGraphFiles)
            {
                var filename = Path.GetFileName(filePath);
                Debug.Log($"ModLoader: Processing game graph file from VFS: {mod.Id}/{filename}");
                processor.ProcessGameGraphFromVFS(vfs, mod.Id, filename);
            }

            Debug.Log($"ModLoader: Successfully applied mod '{mod.Name}' from VFS");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ModLoader: Error applying mod '{mod.Name}' ({mod.Id}) from VFS: {ex.Message}");
            MarkModAsFailed(mod.Id);
        }
    }

    /// <summary>
    /// Mark a mod as failed (prevents dependent mods from loading)
    /// </summary>
    private void MarkModAsFailed(string modId)
    {
        if (failedMods.Add(modId))
        {
            Debug.LogError($"ModLoader: Marking mod '{modId}' as failed");
        }
    }
}
