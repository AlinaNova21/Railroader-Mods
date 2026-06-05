using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.Core;

/// <summary>
/// Interface for working snapshot operations
/// </summary>
public interface IWorkingSnapshot
{
    /// <summary>
    /// Apply a patch to a resource in the snapshot
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceType">Resource type name (e.g., "trackNodes")</param>
    /// <param name="id">Resource identifier</param>
    /// <param name="patch">JObject patch to apply</param>
    void ApplyPatch<T>(string resourceType, string id, JObject patch) where T : class, IResource, new();
    
    /// <summary>
    /// Check if a resource exists in the snapshot
    /// </summary>
    /// <param name="resourceType">Resource type name</param>
    /// <param name="resourceId">Resource identifier</param>
    /// <returns>True if resource exists</returns>
    bool ResourceExists(string resourceType, string resourceId);
    
    /// <summary>
    /// Get a section of resources by type
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="sectionName">Section name</param>
    /// <returns>Dictionary of resources by ID</returns>
    Dictionary<string, T> GetSection<T>(string sectionName) where T : class, IResource;
    
    /// <summary>
    /// Get a specific resource by type and ID
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceType">Resource type name</param>
    /// <param name="id">Resource identifier</param>
    /// <returns>Resource if found, null otherwise</returns>
    T GetResource<T>(string resourceType, string id) where T : class, IResource;
    
    /// <summary>
    /// Fork this snapshot for copy-on-write operations
    /// </summary>
    /// <returns>Forked snapshot</returns>
    IWorkingSnapshot Fork();
}