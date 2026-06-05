using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlinasRailTools.Scripting;

public class LoaderBuilder(Helper helper)
{
  public Helper Helper => helper;
  public Helper Done() => helper;

  public static Dictionary<string, LoaderInstance> AllLoaders = [];
  public static Dictionary<string, LoaderInstance> LoadersCache = [];

  public static void ReloadCaches()
  {
    AllLoaders.Clear();
    UnityEngine.Object
      .FindObjectsByType<LoaderInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)
      .ToList()
      .ForEach(l => AllLoaders[l.identifier] = l);

    LoadersCache = AllLoaders
      .Where(l => l.Value.gameObject.activeInHierarchy)
      .ToDictionary(k => k.Key, v => v.Value);
  }

  private GameObject GetLoaderParent()
  {
    var world = GameObject.Find("World");
    if (world == null)
    {
      world = new GameObject("World");
    }

    var loaderParent = world.transform.Find("Loaders")?.gameObject;
    if (loaderParent == null)
    {
      loaderParent = new GameObject("Loaders");
      loaderParent.transform.SetParent(world.transform);
    }

    return loaderParent;
  }

  public LoaderBuilder GetLoader(string id, Action<LoaderInstanceBuilder> action)
  {
    action(GetLoader(id));
    return this;
  }

  public LoaderInstanceBuilder GetLoader(string id) =>
    LoadersCache.ContainsKey(id) ?
      new LoaderInstanceBuilder(this, LoadersCache[id]) :
      throw new Exception($"Loader with id {id} does not exist");

  public LoaderBuilder CreateLoader(string id, Action<LoaderInstanceBuilder> action)
  {
    action(CreateLoader(id));
    return this;
  }

  public LoaderInstanceBuilder CreateLoader(string id)
  {
    LoaderInstance loader;
    if (!AllLoaders.TryGetValue(id, out loader))
    {
      var go = new GameObject($"Loader {id}");
      go.SetActive(false);
      loader = go.AddComponent<LoaderInstance>();
      loader.identifier = id;

      // Parent under World/Loaders
      var loaderParent = GetLoaderParent();
      loader.transform.SetParent(loaderParent.transform);
    }

    // Don't activate by default - let user call Done(true) if they want activation
    AllLoaders[id] = loader;
    if (loader.gameObject.activeInHierarchy)
    {
      LoadersCache[id] = loader;
    }
    return new LoaderInstanceBuilder(this, loader);
  }

  public LoaderBuilder Loader(string id, Action<LoaderInstanceBuilder> action)
  {
    action(GetOrCreateLoader(id));
    return this;
  }

  public LoaderInstanceBuilder Loader(string id) => GetOrCreateLoader(id);

  public LoaderBuilder GetOrCreateLoader(string id, Action<LoaderInstanceBuilder> action)
  {
    action(GetOrCreateLoader(id));
    return this;
  }

  public LoaderInstanceBuilder GetOrCreateLoader(string id) =>
    LoadersCache.ContainsKey(id) ? GetLoader(id) : CreateLoader(id);

  public LoaderBuilder RemoveLoader(string id)
  {
    if (LoadersCache.ContainsKey(id))
    {
      LoadersCache[id].gameObject.SetActive(false);
      LoadersCache.Remove(id);
    }
    return this;
  }
}
