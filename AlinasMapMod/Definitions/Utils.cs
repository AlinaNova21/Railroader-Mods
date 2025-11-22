using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Caches;
using Game.Progression;
using Model.Ops;
using Serilog;

namespace AlinasMapMod.Definitions;

public class DefinitionUtils
{

  public static string[] ApplyList(string[] vals, Dictionary<string, bool> dict)
  {
    var items = vals.ToHashSet();
    foreach (var pair in dict) {
      var val = pair.Key;
      var include = pair.Value;
      if (include) {
        items.Add(val);
      } else {
        items.Remove(val);
      }
    }
    return items.ToArray();
  }

  private static T[] ApplyList<T>(T[] vals, Dictionary<string, bool> dict)
  {
    var cached = Utils.GetCache<T>();
    var items = vals.ToList();
    foreach (var pair in dict) {
      var id = pair.Key;
      var val = pair.Value;
      if (!cached.TryGetValue(id, out var item)) {
        Log.Warning($"Item not found in cache {cached.GetType().Name}: {id}");
      }

      if (val) {
        if (item != null && !items.Contains(item)) {
          items.Add(item);
        }
      } else {
        items.Remove(item);
      }
    }
    return items.ToArray();
  }

  public static Section[] ApplyList(Section[] sections, Dictionary<string, bool> dict, Dictionary<string, Section> cached = null)
  {
        cached ??= SectionCache.Instance;
        var items = sections.ToDictionary(s => s.identifier, s => s);
    foreach (var pair in dict) {
      var identifier = pair.Key;
      var val = pair.Value;
      if (val) {
        if (!items.ContainsKey(identifier)) {
          if (cached.TryGetValue(identifier, out var item)) {
            items.Add(identifier, item);
          } else {
            Log.Warning("Section not found: {id}", identifier);
          }
        }
      } else {
        items.Remove(identifier);
      }
    }
    return items.Values.ToArray();
  }

  public static MapFeature[] ApplyList(MapFeature[] list, Dictionary<string, bool> dict, Dictionary<string, MapFeature> cached = null)
  {
    cached ??= MapFeatureCache.Instance;
    var items = list.ToDictionary(s => s.identifier, s => s);
    foreach (var pair in dict) {
      var identifier = pair.Key;
      var val = pair.Value;
      if (val && !items.ContainsKey(identifier)) {
        if (cached.TryGetValue(identifier, out var item)) {
          items.Add(identifier, item);
        } else {
          Log.Warning("MapFeature not found: {id}", identifier);
        }
      } else {
        items.Remove(identifier);
      }
    }
    return items.Values.ToArray();
  }

  public static Area[] ApplyList(Area[] list, Dictionary<string, bool> dict, Dictionary<string, Area> cached = null)
  {
    cached ??= AreaCache.Instance;
        var items = list.ToDictionary(s => s.identifier, s => s);
    foreach (var pair in dict) {
      var identifier = pair.Key;
      var val = pair.Value;
      if (val && !items.ContainsKey(identifier)) {
        if (cached.TryGetValue(identifier, out var item)) {
          items.Add(identifier, item);
        } else {
          Log.Warning("Area not found: {id}", identifier);
        }
      } else {
        items.Remove(identifier);
      }
    }
    return items.Values.ToArray();
  }

  public static Industry[] ApplyList(Industry[] list, Dictionary<string, bool> dict, Dictionary<string, Industry> cached = null)
  {
        cached ??= IndustryCache.Instance;
        var items = list.ToDictionary(s => s.identifier, s => s);
    foreach (var pair in dict) {
      var identifier = pair.Key;
      var val = pair.Value;
      if (val && !items.ContainsKey(identifier)) {
        if (cached.TryGetValue(identifier, out var item)) {
          items.Add(identifier, item);
        } else {
          Log.Warning("Industry not found: {id}", identifier);
        }
      } else {
        items.Remove(identifier);
      }
    }
    return items.Values.ToArray();
  }

  public static IndustryComponent[] ApplyList(IndustryComponent[] list, Dictionary<string, bool> dict, Dictionary<string, IndustryComponent> cached = null)
  {
        cached ??= IndustryComponentCache.Instance;
        var items = list.ToDictionary(s => s.Identifier, s => s);
    foreach (var pair in dict) {
      var identifier = pair.Key;
      var val = pair.Value;
      if (val && !items.ContainsKey(identifier)) {
        if (cached.TryGetValue(identifier, out var item)) {
          items.Add(identifier, item);
        } else {
          Log.Warning("IndustryComponent not found: {id}", identifier);
        }
      } else {
        items.Remove(identifier);
      }
    }
    return items.Values.ToArray();
  }
}
