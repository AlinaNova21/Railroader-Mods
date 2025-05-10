using System.Linq;
using Helpers;
using UnityEngine;

namespace MapEditor.StateTracker.Node
{
  public sealed class SceneryGhost(string id)
  {

    internal Vector3 _Position;
    internal Vector3 _Rotation;
    internal Vector3 _Scale;
    internal string _Model = "";

    public SceneryGhost(string id, Vector3 position, Vector3 rotation, Vector3 scale, string model)
      : this(id)
    {
      _Position = position;
      _Rotation = rotation;
      _Scale = scale;
      _Model = model;
    }

    public void UpdateGhost(SceneryAssetInstance scenery)
    {
      _Position = scenery.transform.localPosition;
      _Rotation = scenery.transform.localEulerAngles;
      _Scale = scenery.transform.localScale;
      _Model = scenery.identifier;
    }

    public void UpdateScenery(SceneryAssetInstance scenery)
    {
      scenery.transform.localPosition = _Position;
      scenery.transform.localEulerAngles = _Rotation;
      scenery.transform.localScale = _Scale;
      var reload = scenery.identifier != _Model;
      scenery.identifier = _Model;
      if (reload) {
        scenery.gameObject.SetActive(false);
        scenery.ReloadComponents();
        scenery.gameObject.SetActive(true);
        EditorContext.AttachUiHelper(scenery);
      }
    }

    public void CreateScenery()
    {
      var parent = GameObject.Find("World");
      var go = new GameObject(id);
      go.SetActive(false);
      go.transform.parent = parent.transform;
      var scenery = go.AddComponent<SceneryAssetInstance>();
      scenery.name = id;
      scenery.identifier = _Model;
      UpdateScenery(scenery);
      go.SetActive(true);
      EditorContext.PatchEditor!.AddOrUpdateScenery(id, _Model, _Position, _Rotation, _Scale);
      EditorContext.AttachUiHelper(scenery);
    }

    public void DestroyScenery()
    {
      var scenery = GameObject.FindObjectsOfType<SceneryAssetInstance>().FirstOrDefault(s => s.name == id);
      UpdateGhost(scenery);
      Object.Destroy(scenery.gameObject);
      EditorContext.PatchEditor!.RemoveScenery(id);
    }
  }
}
