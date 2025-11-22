using System;
using System.Collections.Generic;

namespace AlinasMapMod.Tests.Mocks;
    /// <summary>
    /// Mock interface for game cache systems used in validation
    /// </summary>
    /// <typeparam name="T">Type of cached objects</typeparam>
    public interface IMockCache<T>
    {
        bool Contains(string key);
        T Get(string key);
        IEnumerable<string> GetAllKeys();
        bool TryGet(string key, out T value);
        Type GetCachedType(string key);
    }

    /// <summary>
    /// Mock interface for prefab registry used in game object validation
    /// </summary>
    public interface IMockPrefabRegistry
    {
        bool IsValidPrefab(string prefabName);
        IEnumerable<string> GetAvailablePrefabs();
        string GetPrefabCategory(string prefabName);
        bool IsPrefabInCategory(string prefabName, string category);
    }

    /// <summary>
    /// Mock interface for game object resolution used in URI validation
    /// </summary>
    public interface IMockGameObjectResolver
    {
        bool GameObjectExists(string path);
        object ResolveGameObject(string path);
        Type GetGameObjectType(string path);
        bool IsValidPath(string path);
    }

    /// <summary>
    /// Mock interface for scene management used in path validation
    /// </summary>
    public interface IMockSceneManager
    {
        bool IsSceneLoaded(string sceneName);
        IEnumerable<string> GetLoadedScenes();
        bool IsValidScenePath(string scenePath);
    }

    /// <summary>
    /// Mock interface for asset validation used in scenery validation
    /// </summary>
    public interface IMockAssetValidator
    {
        bool IsValidAsset(string assetIdentifier);
        string GetAssetType(string assetIdentifier);
        bool IsAssetAvailable(string assetIdentifier);
        IEnumerable<string> GetAvailableAssets();
    }

    /// <summary>
    /// Mock validation context for testing context-dependent validation
    /// </summary>
    public class MockValidationContext
    {
        public string FieldName { get; set; }
        public string ObjectType { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public object Parent { get; set; }
        public int ValidationDepth { get; set; }
        
        public T GetProperty<T>(string key, T defaultValue = default(T))
        {
            return Properties.ContainsKey(key) ? (T)Properties[key] : defaultValue;
        }
        
        public void SetProperty(string key, object value)
        {
            Properties[key] = value;
        }
    }