
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using AlinasMapMod.Caches;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers;
using Serilog;
using UnityEngine;

namespace AlinasMapMod;

public class Utils
{
  public static readonly Serilog.ILogger logger = Log.ForContext<Utils>();

  internal static Dictionary<string, GameObject> _parents = [];

  public static GameObject GetParent(string id)
  {
    if (_parents.TryGetValue(id, out var go))
      return go;
    var world = GameObject.Find("World");
    var parent = new GameObject(id);
    parent.transform.SetParent(world.transform, false);
    _parents[id] = parent;
    return parent;
  }

  public static Dictionary<string, bool> BoolMapSafe(IList<string> items)
  {
    return BoolMapSafe(items, item => item);
  }
  public static Dictionary<string, bool> BoolMapSafe<V>(IList<V> items, Func<V, string> getKey)
  {
    Dictionary<string, bool> dict = [];
    foreach (var item in items) {
      if (item == null) {
        Log.Warning("Null item found in BoolMapSafe");
        continue;
      }
      var key = getKey(item);
      if (dict.ContainsKey(key)) {
        Log.Warning("Duplicate key found in BoolMapSafe: {key}", key);
        continue;
      } else {
        dict[key] = true;
      }
    }
    return dict;
  }

  public static string GetPathFromGameObject(GameObject go)
  {
    if (go.TryGetComponent<SceneryAssetInstance>(out var sai))
      return "scenery://" + sai.identifier;
    var parts = new List<string>()
    {
      "path://scene",
    };
    parts.AddRange(go.GetComponentsInParent<Transform>(true).Select(t => t.name).Reverse());
    return string.Join("/", parts);
  }

  public static void ValidatePrefab(string prefabUri, string[] vanillaValidList)
  {
    if (string.IsNullOrEmpty(prefabUri))
      throw new ValidationException("Prefab URI cannot be null or empty");
    if (!prefabUri.Contains("://"))
      throw new ValidationException($"Invalid prefab URI: {prefabUri}, must match the pattern of (empty|path|scenery|vanilla)://host/path");
    var scheme = prefabUri.Split(':')[0];
    var host = prefabUri.Substring(prefabUri.IndexOf("://") + 3);
    if (scheme == "vanilla" && !vanillaValidList.Contains(host))
      throw new ValidationException($"Invalid vanilla prefab: {host}, must be one of {string.Join(", ", vanillaValidList)}");
  }

  public static GameObject GameObjectFromUri(string uriString)
  {
    var requiredParts = new string[] { "://" };
    if (String.IsNullOrEmpty(uriString)) throw new ArgumentException("Invalid uri: empty string is not allowed");
    if (!uriString.Contains("://")) throw new ArgumentException($"Invalid uri: '{uriString}', must match the pattern of (empty|path|scenery|vanilla)://host/path");
    var scheme = uriString.Split(':')[0];
    var hostPath = uriString.Split(':')[1];
    var segments = hostPath.Split('/');
    var host = segments[2];
    segments = segments.Skip(3).ToArray();
    switch (scheme) {
      case "path":
        if (host != "scene")
          throw new ArgumentException($"Invalid uri: {uriString}, path uris must start with scene");
        // TODO: Handle objects not found
        var go = GameObject.Find(segments[0]);
        for (int i = 1; i < segments.Length; i++) {
          var trans = go.transform.Find(segments[i]);
          if (!trans) {
            Log.Warning("Object not found: {segment}", segments[i]);
            return null;
          }
          go = trans.gameObject;
        }
        return go;
      case "scenery":
        var scenery = UnityEngine.Object.FindObjectsOfType<SceneryAssetInstance>(true).FirstOrDefault(s => s.name == host);
        if (scenery == null) {
          Log.Warning("Scenery not found: {host}", host);
          return null;
        }
        return scenery.gameObject;
      case "vanilla":
        return VanillaPrefabs.GetPrefab(Uri.UnescapeDataString(host));
      case "empty":
        return new GameObject();
      default:
        throw new ArgumentException("Invalid uri or objecct not found");
    }
  }

  public static T CreateGameObjectComponent<T>(string identifier) where T : MonoBehaviour
  {
    var go = new GameObject(identifier);
    var comp = go.AddComponent<T>();
    return comp;
  }

  public static IObjectCache<T> GetCache<T>() => AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(a => a.GetTypes())
      .Where(t => typeof(IObjectCache<T>).IsAssignableFrom(t))
      .Take(1)
      .Select(t => typeof(IObjectCache<T>).GetProperty("Instance", System.Reflection.BindingFlags.Static).GetMethod.Invoke(null, null) as IObjectCache<T>)
      .SingleOrDefault();
  internal static void ClearCaches()
  {
    _parents.Clear();
    VanillaPrefabs.ClearCache();
  }

#if PRIVATETESTING
  public static T[] GetFromCache<T>(IEnumerable<XElement> elems)
  {
    var cache = GetCache<T>();
    var ret = new List<T>();
    foreach (var elem in elems)
    {
      var id = elem.Attribute("id")?.Value;
      if (id == null)
      {
        logger.Warning($"{typeof(T).Name} missing id");
        continue;
      }
      if (!cache.TryGetValue(id, out var obj))
      {
        logger.Warning($"Cache miss {typeof(T).Name} {id}");
        continue;
      }
      ret.Add(obj);
    }
    return ret.ToArray();
  }
#endif
}
