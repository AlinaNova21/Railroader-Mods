using AlinasRailTools.Extensions;
using AlinasRailTools.Resources;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Resources;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.Managers;

/// <summary>
/// Marker component for scenery objects
/// </summary>
public class SceneryObject : MonoBehaviour
{
  public string ModelIdentifier;
}

/// <summary>
/// Manager for scenery assets
/// No dependencies - scenery is independent
/// </summary>
[ResourceType("scenery")]
public class SceneryManager : BaseResourceManager<SceneryObject>
{
  protected override SceneryObject CreateEntity(string id)
  {
    var go = new GameObject($"Scenery_{id}");
    go.transform.SetParent(transform);
    return go.AddComponent<SceneryObject>();
  }

  protected override void UpdateEntity(
    string id,
    SceneryObject entity,
    JObject data,
    IResourceLocator locator)
  {
    var scenery = data.ToObject<SerializedScenery>();

    entity.ModelIdentifier = scenery.ModelIdentifier;
    entity.gameObject.name = $"Scenery_{scenery.ModelIdentifier}_{id}";
    entity.transform.position = scenery.Position.ToUnity();
    entity.transform.rotation = Quaternion.Euler(scenery.Rotation.ToUnity());
    entity.transform.localScale = scenery.Scale.ToUnity();

    // TODO: Load model asset from scenery.ModelIdentifier
  }

  protected override object Serialize(string id, SceneryObject entity)
  {
    return new SerializedScenery
    {
      ModelIdentifier = entity.ModelIdentifier,
      Position = entity.transform.position.ToSerialized(),
      Rotation = entity.transform.rotation.eulerAngles.ToSerialized(),
      Scale = entity.transform.localScale.ToSerialized()
    };
  }
}
