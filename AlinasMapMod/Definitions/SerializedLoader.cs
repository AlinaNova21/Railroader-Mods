
using AlinasMapMod.Caches;
using AlinasMapMod.Loaders;
using UnityEngine;

namespace AlinasMapMod.Definitions;

[RootObject("loaders")]
public class SerializedLoader :
  ISerializedPatchableComponent<LoaderInstance>,
  ICreatableComponent<LoaderInstance>,
  IDestroyableComponent<LoaderInstance>
{
  public Vector3 Position { get; set; }
  public Vector3 Rotation { get; set; }
  public string Prefab { get; set; } = "empty://";
  public string Industry { get; set; } = "";

  public LoaderInstance Create(string id)
  {
    var go = new GameObject(id);
    go.transform.parent = Utils.GetParent("Loaders").transform;
    var comp = go.AddComponent<LoaderInstance>();
    comp.name = id;
    comp.identifier = id;
    Write(comp);
    LoaderCache.Instance[id] = comp;
    return comp;
  }

  public void Destroy(LoaderInstance comp)
  {
    GameObject.Destroy(comp.gameObject);
    LoaderCache.Instance.Remove(comp.identifier);
  }

  public void Read(LoaderInstance comp)
  {
    Position = comp.transform.localPosition;
    Rotation = comp.transform.localEulerAngles;
    Prefab = comp.Prefab;
    Industry = comp.Industry;
  }

  public void Write(LoaderInstance comp)
  {
    comp.transform.localPosition = Position;
    comp.transform.localEulerAngles = Rotation;
    comp.Prefab = Prefab;
    comp.Industry = Industry;
  }

  public void Validate()
  {
    if (Industry == "" && (Prefab.Contains("coal") || Prefab.Contains("diesel"))) {
      throw new ValidationException("Industry required for prefab " + Prefab);
    }
  }
}
