using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlinasRailTools.Runtime;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat;

/// <summary>
/// Discovers and parses StrangeCustoms mods from definition.json files
/// </summary>
public class ModDiscovery
{
    /// <summary>
    /// Scan a directory for mods with definition.json or art-compat.json files
    /// </summary>
    public Dictionary<string, ModDefinition> DiscoverMods(string modsDirectory)
    {
        var discoveredMods = new Dictionary<string, ModDefinition>();

        Debug.Log($"ModDiscovery: Discovering mods in directory: {Path.GetFullPath(modsDirectory)}");

        if (!Directory.Exists(modsDirectory))
        {
            Debug.LogWarning($"ModDiscovery: Mods directory '{modsDirectory}' not found");
            return discoveredMods;
        }

        var modDirectories = Directory.GetDirectories(modsDirectory);
        Debug.Log($"ModDiscovery: Found {modDirectories.Length} directories in mods folder");

        foreach (var modDir in modDirectories)
        {
            Debug.Log($"ModDiscovery: Checking directory: {modDir}");

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
                Debug.Log($"ModDiscovery: No definition.json or art-compat.json found in {modDir}, skipping");
                continue;
            }

            try
            {
                var definition = ParseModDefinition(pathToUse, modDir);
                if (definition != null)
                {
                    discoveredMods[definition.Id] = definition;
                    Debug.Log($"ModDiscovery: Discovered mod '{definition.Name}' ({definition.Id})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ModDiscovery: Error parsing {pathToUse}: {ex.Message}");
            }
        }

        Debug.Log($"ModDiscovery: Discovered {discoveredMods.Count} valid mods");
        return discoveredMods;
    }

    /// <summary>
    /// Parse a definition.json file into a ModDefinition
    /// </summary>
    private ModDefinition ParseModDefinition(string definitionPath, string modDirectory)
    {
        var jsonText = File.ReadAllText(definitionPath);
        var json = JObject.Parse(jsonText);

        var mod = new ModDefinition
        {
            Id = json["id"]?.ToString(),
            Name = json["name"]?.ToString(),
            Version = json["version"]?.ToString(),
            DirectoryPath = modDirectory
        };

        if (string.IsNullOrEmpty(mod.Id) || string.IsNullOrEmpty(mod.Name))
        {
            Debug.LogWarning($"ModDiscovery: Invalid mod definition in {definitionPath} - missing id or name");
            return null;
        }

        // Parse dependencies from requires array
        var requires = json["requires"];
        if (requires != null)
        {
            foreach (var req in requires)
            {
                string depId = null;

                if (req.Type == JTokenType.String)
                {
                    depId = req.ToString();
                }
                else if (req.Type == JTokenType.Object)
                {
                    depId = req["id"]?.ToString();
                }

                if (!string.IsNullOrEmpty(depId))
                {
                    mod.Dependencies.Add(depId);
                }
            }
        }

        // Parse mixintos
        var mixintos = json["mixintos"];
        if (mixintos != null && mixintos.Type == JTokenType.Object)
        {
            foreach (var mixinto in mixintos.Cast<JProperty>())
            {
                mod.Mixintos[mixinto.Name] = mixinto.Value.ToObject<object>();
            }
        }

        return mod;
    }

    /// <summary>
    /// Discover mods from the VirtualFileSystem
    /// </summary>
    public Dictionary<string, ModDefinition> DiscoverModsFromVFS(VirtualFileSystem vfs)
    {
        var discoveredMods = new Dictionary<string, ModDefinition>();

        Debug.Log("ModDiscovery: Discovering mods from VFS");

        // List all files under sc-mods/ prefix
        var vfsFiles = vfs.ListFiles("sc-mods/").ToList();
        Debug.Log($"ModDiscovery: Found {vfsFiles.Count} files in VFS under sc-mods/");

        // Extract unique mod IDs from paths like "sc-mods/{modid}/definition.json" or "sc-mods/{modid}/art-compat.json"
        var modIds = new HashSet<string>();
        foreach (var path in vfsFiles)
        {
            if (path.Contains("/definition.json") || path.Contains("/art-compat.json"))
            {
                var parts = path.Split('/');
                if (parts.Length >= 2)
                {
                    modIds.Add(parts[1]); // sc-mods/{modid}/...
                }
            }
        }

        Debug.Log($"ModDiscovery: Found {modIds.Count} mods in VFS");

        foreach (var modId in modIds)
        {
            try
            {
                var definition = ParseModDefinitionFromVFS(vfs, modId);
                if (definition != null)
                {
                    discoveredMods[definition.Id] = definition;
                    Debug.Log($"ModDiscovery: Discovered mod '{definition.Name}' ({definition.Id}) from VFS");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ModDiscovery: Error parsing mod {modId} from VFS: {ex.Message}");
            }
        }

        Debug.Log($"ModDiscovery: Discovered {discoveredMods.Count} valid mods from VFS");
        return discoveredMods;
    }

    /// <summary>
    /// Parse a mod definition from VFS
    /// </summary>
    private ModDefinition ParseModDefinitionFromVFS(VirtualFileSystem vfs, string modId)
    {
        // Check for art-compat.json first, then fallback to definition.json
        var artCompatPath = $"sc-mods/{modId}/art-compat.json";
        var definitionPath = $"sc-mods/{modId}/definition.json";

        string pathToUse = null;
        string jsonText = null;

        if (vfs.FileExists(artCompatPath))
        {
            Debug.Log($"ModDiscovery: Found art-compat.json for mod {modId} in VFS");
            pathToUse = artCompatPath;
            jsonText = vfs.ReadFile(artCompatPath);
        }
        else if (vfs.FileExists(definitionPath))
        {
            Debug.Log($"ModDiscovery: Found definition.json for mod {modId} in VFS");
            pathToUse = definitionPath;
            jsonText = vfs.ReadFile(definitionPath);
        } else
        {
            Debug.LogWarning($"ModDiscovery: No definition.json or art-compat.json found for mod {modId} in VFS");
            var files = vfs.ListFiles($"sc-mods/{modId}/").ToList();
            Debug.LogWarning($"ModDiscovery: Files found in sc-mods/{modId}/:\n- {string.Join("\n- ", files)}");
            return null;
        }

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogWarning($"ModDiscovery: Could not read definition.json or art-compat.json for mod {modId} from VFS");
            return null;
        }

        var json = JObject.Parse(jsonText);

        var mod = new ModDefinition
        {
            Id = json["id"]?.ToString(),
            Name = json["name"]?.ToString(),
            Version = json["version"]?.ToString(),
            DirectoryPath = $"vfs://sc-mods/{modId}" // Virtual path indicator
        };

        if (string.IsNullOrEmpty(mod.Id) || string.IsNullOrEmpty(mod.Name))
        {
            Debug.LogWarning($"ModDiscovery: Invalid mod definition in VFS for {modId} - missing id or name");
            return null;
        }

        // Parse dependencies from requires array
        var requires = json["requires"];
        if (requires != null)
        {
            foreach (var req in requires)
            {
                string depId = null;

                if (req.Type == JTokenType.String)
                {
                    depId = req.ToString();
                }
                else if (req.Type == JTokenType.Object)
                {
                    depId = req["id"]?.ToString();
                }

                if (!string.IsNullOrEmpty(depId))
                {
                    mod.Dependencies.Add(depId);
                }
            }
        }

        // Parse mixintos
        var mixintos = json["mixintos"];
        if (mixintos != null && mixintos.Type == JTokenType.Object)
        {
            foreach (var mixinto in mixintos.Cast<JProperty>())
            {
                mod.Mixintos[mixinto.Name] = mixinto.Value.ToObject<object>();
            }
        }

        return mod;
    }
}
