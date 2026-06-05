using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AlinasRailTools.Core;

/// <summary>
/// Working snapshot implementation that supports copy-on-write operations and validation
/// </summary>
public class WorkingSnapshot : IWorkingSnapshot
{
    private static readonly ILogger Logger = Log.ForContext<WorkingSnapshot>();
    
    private readonly Dictionary<string, Dictionary<string, object>> _sections;
    private readonly WorkingSnapshot _parent;

    /// <summary>
    /// Create a root working snapshot
    /// </summary>
    public WorkingSnapshot()
    {
        _sections = new Dictionary<string, Dictionary<string, object>>();
        _parent = null;
    }

    /// <summary>
    /// Create a forked working snapshot (copy-on-write)
    /// </summary>
    /// <param name="parent">Parent snapshot to fork from</param>
    private WorkingSnapshot(WorkingSnapshot parent)
    {
        _sections = new Dictionary<string, Dictionary<string, object>>();
        _parent = parent;
    }

    /// <summary>
    /// Apply a patch to a resource in the snapshot
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceType">Resource type name (e.g., "trackNodes")</param>
    /// <param name="id">Resource identifier</param>
    /// <param name="patch">JObject patch to apply</param>
    public void ApplyPatch<T>(string resourceType, string id, JObject patch) where T : class, IResource, new()
    {
        Logger.Debug("Applying patch to {ResourceType}/{ResourceId}", resourceType, id);
        
        EnsureSection(resourceType);

        // Check if patch has _delete flag
        if (patch.TryGetValue("_delete", out var deleteToken) && deleteToken.Value<bool>())
        {
            // Remove the resource from this snapshot
            _sections[resourceType].Remove(id);
            Logger.Debug("Deleted resource {ResourceType}/{ResourceId}", resourceType, id);
            return;
        }

        // Get existing resource or create new one
        var resource = GetResource<T>(resourceType, id) ?? new T { Id = id };
        var isNewResource = GetResource<T>(resourceType, id) == null;

        // Apply the patch using JsonConvert.PopulateObject
        JsonConvert.PopulateObject(patch.ToString(), resource);

        // Store the updated resource (copy-on-write)
        _sections[resourceType][id] = resource;

        Logger.Debug("{Action} resource {ResourceType}/{ResourceId}", 
            isNewResource ? "Created" : "Updated", resourceType, id);

        // Validate the resource after patching
        var validationResult = resource.Validate(this);
        if (!validationResult.IsValid)
        {
            Logger.Warning("Validation failed for {ResourceType}/{ResourceId}: {Errors}", 
                resourceType, id, validationResult.Errors);
            throw new ValidationException($"Resource {resourceType}/{id} failed validation after patch", validationResult.Errors);
        }
    }

    /// <summary>
    /// Check if a resource exists in the snapshot
    /// </summary>
    /// <param name="resourceType">Resource type name</param>
    /// <param name="resourceId">Resource identifier</param>
    /// <returns>True if resource exists</returns>
    public bool ResourceExists(string resourceType, string resourceId)
    {
        // Check this snapshot first
        if (_sections.TryGetValue(resourceType, out var section) && section.ContainsKey(resourceId))
        {
            return true;
        }

        // Check parent snapshots
        return _parent?.ResourceExists(resourceType, resourceId) ?? false;
    }

    /// <summary>
    /// Get a section of resources by type
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="sectionName">Section name</param>
    /// <returns>Dictionary of resources by ID</returns>
    public Dictionary<string, T> GetSection<T>(string sectionName) where T : class, IResource
    {
        var result = new Dictionary<string, T>();

        // Collect from parent first (if exists)
        if (_parent != null)
        {
            var parentSection = _parent.GetSection<T>(sectionName);
            foreach (var kvp in parentSection)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        // Overlay with this snapshot's resources
        if (_sections.TryGetValue(sectionName, out var section))
        {
            foreach (var kvp in section)
            {
                if (kvp.Value is T resource)
                {
                    result[kvp.Key] = resource;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get a specific resource by type and ID
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceType">Resource type name</param>
    /// <param name="id">Resource identifier</param>
    /// <returns>Resource if found, null otherwise</returns>
    public T GetResource<T>(string resourceType, string id) where T : class, IResource
    {
        // Check this snapshot first
        if (_sections.TryGetValue(resourceType, out var section) && section.TryGetValue(id, out var resource))
        {
            return resource as T;
        }

        // Check parent snapshots
        return _parent?.GetResource<T>(resourceType, id);
    }

    /// <summary>
    /// Fork this snapshot for copy-on-write operations
    /// </summary>
    /// <returns>Forked snapshot</returns>
    public IWorkingSnapshot Fork()
    {
        return new WorkingSnapshot(this);
    }

    /// <summary>
    /// Ensure a section exists in this snapshot
    /// </summary>
    /// <param name="sectionName">Section name to ensure exists</param>
    private void EnsureSection(string sectionName)
    {
        if (!_sections.ContainsKey(sectionName))
        {
            _sections[sectionName] = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Add a resource directly to the snapshot (for initialization)
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceType">Resource type name</param>
    /// <param name="resource">Resource to add</param>
    public void AddResource<T>(string resourceType, T resource) where T : class, IResource
    {
        EnsureSection(resourceType);
        _sections[resourceType][resource.Id] = resource;
    }

    /// <summary>
    /// Get all section names in this snapshot and its parents
    /// </summary>
    /// <returns>Set of section names</returns>
    public HashSet<string> GetSectionNames()
    {
        var result = new HashSet<string>(_sections.Keys);
        
        if (_parent != null)
        {
            result.UnionWith(_parent.GetSectionNames());
        }

        return result;
    }
}