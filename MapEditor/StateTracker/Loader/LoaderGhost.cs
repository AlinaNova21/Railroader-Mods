using System.Collections.Generic;
using AlinasMapMod.Definitions;
using MapEditor.Extensions;
using Newtonsoft.Json.Linq;
using Track;
using UnityEngine;
using static AlinasMapMod.LoaderBuilder;

namespace MapEditor.StateTracker.Node
{
  public sealed class LoaderGhost(string id)
  {

    internal Vector3 _Position;
    internal Vector3 _Rotation;
    internal string _Prefab = "";
    internal string _Industry = "";

    public LoaderGhost(string id, Vector3 position, Vector3 rotation, string prefab, string industry)
      : this(id)
    {
      _Position = position;
      _Rotation = rotation;
      _Prefab = prefab;
      _Industry = industry;
    }

    public void UpdateGhost(CustomLoader loader)
    {
      _Position = loader.transform.localPosition;
      _Rotation = loader.transform.localEulerAngles;
      _Prefab = loader.config.Prefab;
      _Industry = loader.config.Industry;
    }

    public void UpdateLoader(CustomLoader loader)
    {
      loader.transform.localPosition = _Position;
      loader.transform.localEulerAngles = _Rotation;
      loader.config.Position = _Position;
      loader.config.Rotation = _Rotation;
      var needsRebuild = false;
      if (loader.config.Prefab != _Prefab || loader.config.Industry != _Industry)
      {
        needsRebuild = true;
      }
      loader.config.Prefab = _Prefab;
      loader.config.Industry = _Industry;
      if (needsRebuild)
      {
        loader.Rebuild();
      }
    }

    public void CreateLoader()
    {
      var parent = GameObject.Find("Large Scenery");
      var go = new GameObject(id);
      go.transform.parent = parent.transform;
      var cl = go.AddComponent<CustomLoader>();
      cl.id = id;
      cl.config = new SerializedLoader();
      UpdateLoader(cl);
      cl.Rebuild();
      EditorContext.PatchEditor!.AddOrUpdateSpliney(id, (_) => GetSpliney());
      EditorContext.AttachUiHelper(cl);
    }

    public void DestroyLoader()
    {
      var cl = CustomLoader.FindById(id);
      if (cl == null)
      {
        return;
      }
      UpdateGhost(cl);
      Object.Destroy(cl.gameObject);
      EditorContext.PatchEditor!.RemoveSpliney(id);
    }

    public JObject GetSpliney()
    {
      var data = new SerializedLoader
      {
        Prefab = _Prefab,
        Industry = _Industry,
        Position = _Position,
        Rotation = _Rotation
      };
      var raw = JObject.FromObject(data);
      raw.Add("Handler", "AlinasMapMod.LoaderBuilder");
      return raw;
    }

  }
}
