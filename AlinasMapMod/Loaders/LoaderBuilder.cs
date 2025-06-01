using System;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace AlinasMapMod.Loaders;

public partial class LoaderBuilder : StrangeCustoms.ISplineyBuilder, IObjectFactory
{
  readonly Serilog.ILogger logger = Log.ForContext<LoaderBuilder>();

  public bool Enabled => true;

  public string Name => "Loader";

  public Type ObjectType => typeof(LoaderInstance);

  public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
  {
    var loader = data.ToObject<SerializedLoader>();
    logger.Information($"Configuring loader {id} with prefab {loader.Prefab}");
    try {
      loader.Validate();
      return loader.Create(id).gameObject;
    } catch (Exception ex) {
      logger.Error(ex, "Failed to create loader {Id}", id);
      throw new InvalidOperationException($"Failed to create loader {id}", ex);
    }
  }

  public IEditableObject CreateObject(PatchEditor editor, string id) => new SerializedLoader
  {
    Prefab = "vanilla://waterTower"
  }.Create(id);
}
