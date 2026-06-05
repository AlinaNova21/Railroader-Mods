using Helpers;
using UnityEngine;

namespace AlinasRailTools.Scripting.Ops;

public class SceneryAssetInstanceBuilder(SceneryBuilder parent, SceneryAssetInstance scenery)
  : BaseBuilder<SceneryBuilder, SceneryAssetInstanceBuilder>(parent, scenery.identifier)
{
  private readonly SceneryAssetInstance _scenery = scenery;

  public SceneryAssetInstanceBuilder ModelIdentifier(string modelIdentifier)
  {
    _scenery.identifier = modelIdentifier;
    return this;
  }

  public SceneryAssetInstanceBuilder Position(Vector3 position)
  {
    _scenery.transform.position = position;
    return this;
  }

  public SceneryAssetInstanceBuilder Rotation(Vector3 rotation)
  {
    _scenery.transform.rotation = Quaternion.Euler(rotation);
    return this;
  }

  public SceneryAssetInstanceBuilder Scale(Vector3 scale)
  {
    _scenery.transform.localScale = scale;
    return this;
  }

  public override SceneryBuilder Remove()
  {
    Parent.RemoveScenery(Id);
    return Parent;
  }

  public override SceneryAssetInstanceBuilder SetActive(bool active = true)
  {
    _scenery.gameObject.SetActive(active);
    return this;
  }
}