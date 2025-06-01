using AlinasMapMod.Definitions;
using AlinasMapMod.Loaders;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MapEditor.StateTracker.Node;

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

  public void UpdateGhost(LoaderInstance loader)
  {
    _Position = loader.transform.localPosition;
    _Rotation = loader.transform.localEulerAngles;
    _Prefab = loader.Prefab;
    _Industry = loader.Industry;
  }

  public void UpdateLoader(LoaderInstance loader)
  {
    loader.transform.localPosition = _Position;
    loader.transform.localEulerAngles = _Rotation;
    loader.Prefab = _Prefab;
    loader.Industry = _Industry;
  }

  public void CreateLoader()
  {
    var loader = new SerializedLoader().Create(id);
    UpdateLoader(loader);
    EditorContext.PatchEditor!.AddOrUpdateSpliney(id, (_) => GetSpliney());
    EditorContext.AttachUiHelper(loader);
  }

  public void DestroyLoader()
  {
    var cl = LoaderInstance.FindById(id);
    if (cl == null) {
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
