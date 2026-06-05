using System;
using System.Collections.Generic;
using System.IO;
using AlinasRailTools.Runtime;
using AlinasRailTools.Scripting;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AlinasRailTools.StrangeCustomsCompat;

public class ModVFSPreloader
{
    private static readonly ILogger Logger = Log.ForContext<ModVFSPreloader>();

    public List<string> PreloadModsToVFS(VirtualFileSystem vfs, string modsDirectory)
    {
        var loadedModIds = new List<string>();
        var totalFilesWritten = 0;

        if (!Directory.Exists(modsDirectory))
        {
            Logger.Warning("Mods directory not found: {ModsDirectory}", modsDirectory);
            return loadedModIds;
        }

        var modDirectories = Directory.GetDirectories(modsDirectory);
        Logger.Information("Scanning {Count} mod directories for preloading", modDirectories.Length);

        foreach (var modDir in modDirectories)
        {
            try
            {
                // Check for art-compat.json first, then fallback to definition.json
                var artCompatPath = Path.Combine(modDir, "art-compat.json");
                var definitionPath = Path.Combine(modDir, "definition.json");

                string pathToUse = null;
                if (File.Exists(artCompatPath))
                {
                    pathToUse = artCompatPath;
                }
                else if (File.Exists(definitionPath))
                {
                    pathToUse = definitionPath;
                }

                if (pathToUse == null)
                {
                    Logger.Debug("Skipping {ModDir} - no definition.json or art-compat.json found", modDir);
                    continue;
                }

                // Load and parse definition
                var definitionJson = File.ReadAllText(pathToUse);
                var definition = JObject.Parse(definitionJson);

                var modId = definition["id"]?.ToString();
                if (string.IsNullOrEmpty(modId))
                {
                    Logger.Warning("Skipping {ModDir} - definition missing 'id' field", modDir);
                    continue;
                }

                Logger.Information("Preloading mod {ModId} to VFS", modId);
                var modFilesWritten = 0;

                // Write definition to VFS (always as definition.json for compatibility)
                var vfsDefPath = $"sc-mods/{modId}/definition.json";
                vfs.WriteFile(vfsDefPath, definitionJson);
                Logger.Debug("Wrote {Path} to VFS", vfsDefPath);
                modFilesWritten++;
                totalFilesWritten++;

                // Process mixintos
                var mixintos = definition["mixintos"] as JObject;
                if (mixintos != null)
                {
                    ProcessMixinto(vfs, mixintos, "game-graph", modId, modDir, ref modFilesWritten, ref totalFilesWritten);
                    ProcessMixinto(vfs, mixintos, "progressions", modId, modDir, ref modFilesWritten, ref totalFilesWritten);
                }

                Logger.Debug("Wrote {Count} files for mod {ModId}", modFilesWritten, modId);
                loadedModIds.Add(modId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to preload mod from {ModDir}", modDir);
            }
        }

        Logger.Information("Preloaded {ModCount} mods ({FileCount} total files) to VFS", loadedModIds.Count, totalFilesWritten);
        return loadedModIds;
    }

    private void ProcessMixinto(VirtualFileSystem vfs, JObject mixintos, string mixintoKey, string modId, string modDir, ref int modFilesWritten, ref int totalFilesWritten)
    {
        if (!mixintos.ContainsKey(mixintoKey))
        {
            return;
        }

        var files = new List<string>();
        var mixintoData = mixintos[mixintoKey];

        if (mixintoData is JArray mixintoArray)
        {
            foreach (var item in mixintoArray)
            {
                files.Add(SCCompatHelpers.StripFileWrapper(item.ToString()));
            }
        }
        else if (mixintoData is JValue mixintoValue)
        {
            files.Add(SCCompatHelpers.StripFileWrapper(mixintoValue.ToString()));
        }

        // Load each file into VFS
        foreach (var file in files)
        {
            var filePath = Path.Combine(modDir, file);
            if (File.Exists(filePath))
            {
                var fileContent = File.ReadAllText(filePath);
                var filename = Path.GetFileName(file);
                var vfsPath = $"sc-mods/{modId}/{mixintoKey}/{filename}";
                vfs.WriteFile(vfsPath, fileContent);
                Logger.Debug("Wrote {Path} to VFS", vfsPath);
                modFilesWritten++;
                totalFilesWritten++;
            }
            else
            {
                Logger.Warning("{MixintoKey} file not found: {Path}", mixintoKey, filePath);
            }
        }
    }
}
