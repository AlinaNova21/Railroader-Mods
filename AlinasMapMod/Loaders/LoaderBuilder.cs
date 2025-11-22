using System;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Validation;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace AlinasMapMod.Loaders;

public partial class LoaderBuilder : SplineyBuilderBase, IObjectFactory
{
  public string Name => "Loader";
  public bool Enabled => true;
  public Type ObjectType => typeof(LoaderInstance);

  protected override GameObject BuildSplineyInternal(string id, Transform parentTransform, JObject data)
  {
    return BuildFromCreatableComponent<LoaderInstance, SerializedLoader>(id, data);
  }

  public IEditableObject CreateObject(PatchEditor editor, string id) => new SerializedLoader
  {
    Prefab = "vanilla://waterTower"
  }.Create(id);
}
