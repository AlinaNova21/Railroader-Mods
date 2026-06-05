using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using Tomlyn;
using Serilog;

namespace AlinasRailTools.Core;

/// <summary>
/// Loads patch files in multiple formats (JSON, YAML, TOML) and applies them to working snapshots
/// </summary>
public class PatchLoader
{
    private static readonly ILogger Logger = Log.ForContext<PatchLoader>();
    
    private readonly IDeserializer _yamlDeserializer;
    private readonly ResourceRegistry _registry;

    public PatchLoader()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        _registry = ResourceRegistry.Instance;
    }

    /// <summary>
    /// Load and apply patches from a file to the working snapshot
    /// </summary>
    /// <param name="filePath">Path to the patch file</param>
    /// <param name="workingSnapshot">Working snapshot to apply patches to</param>
    /// <returns>Number of patches applied</returns>
    public int LoadPatches(string filePath, IWorkingSnapshot workingSnapshot)
    {
        Logger.Information("Loading patches from {FilePath}", filePath);
        
        if (!File.Exists(filePath))
        {
            Logger.Error("Patch file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Patch file not found: {filePath}");
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var content = File.ReadAllText(filePath);

        Logger.Debug("Loading patches from {Format} file", extension);

        JObject patchData = extension switch
        {
            ".json" => LoadJsonPatches(content),
            ".yaml" or ".yml" => LoadYamlPatches(content),
            ".toml" => LoadTomlPatches(content),
            _ => throw new NotSupportedException($"Unsupported patch file format: {extension}")
        };

        var patchCount = ApplyPatches(patchData, workingSnapshot);
        Logger.Information("Successfully loaded {PatchCount} patches from {FilePath}", patchCount, filePath);
        
        return patchCount;
    }

    /// <summary>
    /// Load patches from a JSON string
    /// </summary>
    /// <param name="jsonContent">JSON content</param>
    /// <param name="workingSnapshot">Working snapshot to apply patches to</param>
    /// <returns>Number of patches applied</returns>
    public int LoadJsonPatches(string jsonContent, IWorkingSnapshot workingSnapshot)
    {
        var patchData = LoadJsonPatches(jsonContent);
        return ApplyPatches(patchData, workingSnapshot);
    }

    /// <summary>
    /// Load patches from a YAML string
    /// </summary>
    /// <param name="yamlContent">YAML content</param>
    /// <param name="workingSnapshot">Working snapshot to apply patches to</param>
    /// <returns>Number of patches applied</returns>
    public int LoadYamlPatches(string yamlContent, IWorkingSnapshot workingSnapshot)
    {
        var patchData = LoadYamlPatches(yamlContent);
        return ApplyPatches(patchData, workingSnapshot);
    }

    /// <summary>
    /// Load patches from a TOML string
    /// </summary>
    /// <param name="tomlContent">TOML content</param>
    /// <param name="workingSnapshot">Working snapshot to apply patches to</param>
    /// <returns>Number of patches applied</returns>
    public int LoadTomlPatches(string tomlContent, IWorkingSnapshot workingSnapshot)
    {
        var patchData = LoadTomlPatches(tomlContent);
        return ApplyPatches(patchData, workingSnapshot);
    }

    /// <summary>
    /// Parse JSON content into patch data
    /// </summary>
    /// <param name="jsonContent">JSON content</param>
    /// <returns>Parsed patch data</returns>
    private JObject LoadJsonPatches(string jsonContent)
    {
        try
        {
            return JObject.Parse(jsonContent);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in patch file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parse YAML content into patch data
    /// </summary>
    /// <param name="yamlContent">YAML content</param>
    /// <returns>Parsed patch data</returns>
    private JObject LoadYamlPatches(string yamlContent)
    {
        try
        {
            var yamlObject = _yamlDeserializer.Deserialize(yamlContent);
            var json = JsonConvert.SerializeObject(yamlObject);
            return JObject.Parse(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid YAML in patch file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parse TOML content into patch data
    /// </summary>
    /// <param name="tomlContent">TOML content</param>
    /// <returns>Parsed patch data</returns>
    private JObject LoadTomlPatches(string tomlContent)
    {
        try
        {
            var tomlTable = Toml.Parse(tomlContent).ToModel();
            var json = JsonConvert.SerializeObject(tomlTable);
            return JObject.Parse(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid TOML in patch file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Apply parsed patch data to the working snapshot
    /// </summary>
    /// <param name="patchData">Parsed patch data</param>
    /// <param name="workingSnapshot">Working snapshot to apply patches to</param>
    /// <returns>Number of patches applied</returns>
    private int ApplyPatches(JObject patchData, IWorkingSnapshot workingSnapshot)
    {
        int patchCount = 0;
        var errors = new List<string>();

        Logger.Debug("Applying patches from parsed data with {SectionCount} sections", patchData.Count);

        foreach (var section in patchData)
        {
            var sectionName = section.Key;
            
            // Validate section name
            if (!_registry.IsValidSectionName(sectionName))
            {
                var error = $"Unknown resource section: {sectionName}";
                Logger.Warning(error);
                errors.Add(error);
                continue;
            }

            // Get resource type for this section
            var resourceType = _registry.GetResourceType(sectionName);
            if (resourceType == null)
            {
                var error = $"No resource type registered for section: {sectionName}";
                Logger.Error(error);
                errors.Add(error);
                continue;
            }

            Logger.Debug("Processing section {SectionName} with resource type {ResourceType}", 
                sectionName, resourceType.Name);

            // Apply patches for each resource in the section
            if (section.Value is JObject sectionObj)
            {
                foreach (var resource in sectionObj)
                {
                    var resourceId = resource.Key;
                    if (resource.Value is JObject resourcePatch)
                    {
                        try
                        {
                            // Use reflection to call the generic ApplyPatch method
                            var applyPatchMethod = typeof(IWorkingSnapshot)
                                .GetMethod(nameof(IWorkingSnapshot.ApplyPatch))
                                .MakeGenericMethod(resourceType);

                            applyPatchMethod.Invoke(workingSnapshot, new object[] { sectionName, resourceId, resourcePatch });
                            patchCount++;
                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException is ValidationException validationEx)
                            {
                                var error = $"Validation failed for {sectionName}/{resourceId}: {string.Join(", ", validationEx.ValidationErrors)}";
                                Logger.Warning(error);
                                errors.Add(error);
                            }
                            else
                            {
                                var error = $"Failed to apply patch for {sectionName}/{resourceId}: {ex.InnerException?.Message ?? ex.Message}";
                                Logger.Error(ex, error);
                                errors.Add(error);
                            }
                        }
                    }
                }
            }
        }

        // If there were errors, throw an exception with all the details
        if (errors.Count > 0)
        {
            Logger.Error("Patch loading failed with {ErrorCount} errors", errors.Count);
            throw new InvalidOperationException($"Patch loading failed with {errors.Count} errors:\n" + string.Join("\n", errors));
        }

        Logger.Debug("Successfully applied {PatchCount} patches", patchCount);
        return patchCount;
    }
}