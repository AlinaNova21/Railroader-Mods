using System;
using System.Collections.Generic;
using System.Linq;
using Game.Progression;
using Helpers;
using Model.OpsNew;
using Serilog;
using UnityEngine;

namespace AlinasMapMod.Definitions
{
  public class Utils
  {

    public static string[] ApplyList(string[] vals, Dictionary<string, bool> dict)
    {
      var items = vals.ToHashSet();
      foreach (var pair in dict)
      {
        var val = pair.Key;
        var include = pair.Value;
        if (include)
        {
          items.Add(val);
        }
        else
        {
          items.Remove(val);
        }
      }
      return items.ToArray();
    }

    public static Section[] ApplyList(Section[] sections, Dictionary<string, bool> dict, Dictionary<string, Section> cached)
    {
      var items = sections.ToDictionary(s => s.identifier, s => s);
      foreach (var pair in dict)
      {
        var identifier = pair.Key;
        var val = pair.Value;
        if (val)
        {
          if (!items.ContainsKey(identifier))
          {
            if (cached.TryGetValue(identifier, out var item))
            {
              items.Add(identifier, item);
            }
            else
            {
              Log.Warning("Section not found: {id}", identifier);
            }
          }
        }
        else
        {
          items.Remove(identifier);
        }
      }
      return items.Values.ToArray();
    }

    public static MapFeature[] ApplyList(MapFeature[] list, Dictionary<string, bool> dict, Dictionary<string, MapFeature> cached)
    {
      var items = list.ToDictionary(s => s.identifier, s => s);
      foreach (var pair in dict)
      {
        var identifier = pair.Key;
        var val = pair.Value;
        if (val && !items.ContainsKey(identifier))
        {
          if (cached.TryGetValue(identifier, out var item))
          {
            items.Add(identifier, item);
          }
          else
          {
            Log.Warning("MapFeature not found: {id}", identifier);
          }
        }
        else
        {
          items.Remove(identifier);
        }
      }
      return items.Values.ToArray();
    }

    public static Area[] ApplyList(Area[] list, Dictionary<string, bool> dict, Dictionary<string, Area> cached)
    {
      var items = list.ToDictionary(s => s.identifier, s => s);
      foreach (var pair in dict)
      {
        var identifier = pair.Key;
        var val = pair.Value;
        if (val && !items.ContainsKey(identifier))
        {
          if (cached.TryGetValue(identifier, out var item))
          {
            items.Add(identifier, item);
          }
          else
          {
            Log.Warning("Area not found: {id}", identifier);
          }
        }
        else
        {
          items.Remove(identifier);
        }
      }
      return items.Values.ToArray();
    }

    public static Industry[] ApplyList(Industry[] list, Dictionary<string, bool> dict, Dictionary<string, Industry> cached)
    {
      var items = list.ToDictionary(s => s.identifier, s => s);
      foreach (var pair in dict)
      {
        var identifier = pair.Key;
        var val = pair.Value;
        if (val && !items.ContainsKey(identifier))
        {
          if (cached.TryGetValue(identifier, out var item))
          {
            items.Add(identifier, item);
          }
          else
          {
            Log.Warning("Industry not found: {id}", identifier);
          }
        }
        else
        {
          items.Remove(identifier);
        }
      }
      return items.Values.ToArray();
    }

    public static IndustryComponent[] ApplyList(IndustryComponent[] list, Dictionary<string, bool> dict, Dictionary<string, IndustryComponent> cached)
    {
      var items = list.ToDictionary(s => s.Identifier, s => s);
      foreach (var pair in dict)
      {
        var identifier = pair.Key;
        var val = pair.Value;
        if (val && !items.ContainsKey(identifier))
        {
          if (cached.TryGetValue(identifier, out var item))
          {
            items.Add(identifier, item);
          }
          else
          {
            Log.Warning("IndustryComponent not found: {id}", identifier);
          }
        }
        else
        {
          items.Remove(identifier);
        }
      }
      return items.Values.ToArray();
    }
    public static string getPathFromGameObject(GameObject go)
    {
      if (go.TryGetComponent<SceneryAssetInstance>(out var sai))
        return "scenery://" + sai.identifier;
      return string.Join("/", "path://scene" + go.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray()) + "/" + go.name;
    }

    public static GameObject gameObjectFromUri(string uriString)
    {
      var uri = new Uri(uriString);
      var segments = uri.Segments.Skip(1).Select(s => s.Trim('/')).ToArray();
      switch (uri.Scheme)
      {
        case "path":
          if (uri.Host != "scene")
            throw new ArgumentException("Invalid uri");
          // TODO: Handle objects not found
          var go = GameObject.Find(segments[0]);
          for (int i = 1; i < segments.Length; i++)
          {
            go = go.transform.Find(segments[i]).gameObject;
          }
          return go;
        case "scenery":
          return UnityEngine.Object.FindObjectsOfType<SceneryAssetInstance>(true).First(s => s.identifier == uri.Host).gameObject;
        default:
          throw new ArgumentException("Invalid uri or objecct not found");
      }
    }
  }
}
