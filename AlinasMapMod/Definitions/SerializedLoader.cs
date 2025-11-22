using AlinasMapMod.Caches;
using AlinasMapMod.Loaders;
using AlinasMapMod.Validation;
using UnityEngine;

namespace AlinasMapMod.Definitions;

[RootObject("loaders")]
public class SerializedLoader : SerializedComponentBase<LoaderInstance>,
  ICreatableComponent<LoaderInstance>,
  IDestroyableComponent<LoaderInstance>
{
  public Vector3 Position { get; set; }
  public Vector3 Rotation { get; set; }
  public string Prefab { get; set; } = "empty://";
  public string Industry { get; set; } = "";

  protected override void ConfigureValidation()
  {
    RuleFor(() => Prefab)
      .Required()
      .AsGameObjectUri(VanillaPrefabs.AvailableLoaderPrefabs)
      .Custom((prefab, context) =>
      {
        var result = new ValidationResult { IsValid = true };
        
        // Business logic validation: coal/diesel prefabs require industry
        if (string.IsNullOrEmpty(Industry) && 
            (prefab.Contains("coal") || prefab.Contains("diesel")))
        {
          result.IsValid = false;
          result.Errors.Add(new ValidationError
          {
            Field = "Industry",
            Message = $"Industry required for prefab {prefab}",
            Code = "INDUSTRY_REQUIRED_FOR_PREFAB",
            Value = Industry
          });
        }
        
        return result;
      });

    RuleFor(() => Position)
      .Required();
      
    RuleFor(() => Rotation)
      .Required();
  }

  public override LoaderInstance Create(string id)
  {
    GameObject go = null;
    try
    {
      go = new GameObject(id);
      go.transform.parent = Utils.GetParent("Loaders").transform;
      var comp = go.AddComponent<LoaderInstance>();
      comp.name = id;
      comp.identifier = id;
      Write(comp);
      LoaderCache.Instance[id] = comp;
      return comp;
    }
    catch
    {
      // Clean up the GameObject if creation failed
      if (go != null)
      {
        UnityEngine.Object.DestroyImmediate(go);
      }
      // Remove from cache if it was added
      LoaderCache.Instance.Remove(id);
      throw;
    }
  }

  public void Destroy(LoaderInstance comp)
  {
    GameObject.Destroy(comp.gameObject);
    LoaderCache.Instance.Remove(comp.identifier);
  }

  public override void Read(LoaderInstance comp)
  {
    Position = comp.transform.localPosition;
    Rotation = comp.transform.localEulerAngles;
    Prefab = comp.Prefab;
    Industry = comp.Industry;
  }

  public override void Write(LoaderInstance comp)
  {
    comp.transform.localPosition = Position;
    comp.transform.localEulerAngles = Rotation;
    comp.Prefab = Prefab;
    comp.Industry = Industry;
  }
}
