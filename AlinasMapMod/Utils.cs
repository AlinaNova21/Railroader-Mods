
using System;
using System.Linq;
using System.Reflection;
using Helpers;
using Serilog;
using UnityEngine;

namespace AlinasMapMod
{
  public class Utils
  {
    public static string GetPathFromGameObject(GameObject go)
    {
      if (go.TryGetComponent<SceneryAssetInstance>(out var sai))
        return "scenery://" + sai.identifier;
      return string.Join("/", "path://scene" + go.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray()) + "/" + go.name;
    }

    public static GameObject GameObjectFromUri(string uriString)
    {
      var scheme = uriString.Split(':')[0];
      var hostPath = uriString.Split(':')[1];
      var segments = hostPath.Split('/');
      var host = segments[2];
      segments = segments.Skip(3).ToArray();
      switch (scheme)
      {
        case "path":
          if (host != "scene")
            throw new ArgumentException($"Invalid uri: {uriString}, path uris must start with scene");
          // TODO: Handle objects not found
          var go = GameObject.Find(segments[0]);
          for (int i = 1; i < segments.Length; i++)
          {
            var trans = go.transform.Find(segments[i]);
            if (!trans) {
              Log.Warning("Object not found: {segment}", segments[i]);
              return null;
            }
            go = trans.gameObject;
          }
          return go;
        case "scenery":
          return UnityEngine.Object.FindObjectsOfType<SceneryAssetInstance>(true).First(s => s.name == host).gameObject;
        case "vanilla":
          return VanillaPrefabs.GetPrefab(Uri.UnescapeDataString(host));
        case "empty":
          return new GameObject();
        default:
          throw new ArgumentException("Invalid uri or objecct not found");
      }
    }
  }
}
