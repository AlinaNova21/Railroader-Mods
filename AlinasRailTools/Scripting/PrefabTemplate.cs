using UnityEngine;

namespace AlinasRailTools.Scripting;

public class PrefabTemplate : BaseBuilder<PrefabBuilder, PrefabTemplate>
{
  public GameObject Template { get; }
  public Helper Helper => Parent.Helper;

  public PrefabTemplate(PrefabBuilder parent, GameObject template)
    : base(parent, template.name) => Template = template;

  public override PrefabBuilder Remove() => Parent.RemoveTemplate(Id);

  public override PrefabTemplate Invalidate() => this; // Templates don't need invalidation

  // Properties for GameObject
  public string Name { get => Template.name; set => Template.name = value; }
  public Vector3 Position { get => Template.transform.position; set => Template.transform.position = value; }
  public Quaternion Rotation { get => Template.transform.rotation; set => Template.transform.rotation = value; }
  public Vector3 Scale { get => Template.transform.localScale; set => Template.transform.localScale = value; }

  // Template creation method
  public PrefabTemplate From(GameObject go)
  {
    // Clone the source GameObject into our template
    var cloned = Object.Instantiate(go, Template.transform.parent);
    cloned.name = Template.name; // Keep the template name
    cloned.SetActive(false); // Templates should be inactive

    // Replace the old template GameObject
    if (Template != cloned)
    {
      Object.DestroyImmediate(Template);
    }

    // Update caches
    PrefabBuilder.AllTemplates[Id] = cloned;
    PrefabBuilder.Templates[Id] = cloned;

    return new PrefabTemplate(Parent, cloned);
  }

  // Fluent methods
  public PrefabTemplate WithName(string name) { Name = name; return this; }
  public PrefabTemplate At(Vector3 position) { Position = position; return this; }
  public PrefabTemplate At(float x, float y, float z) => At(new Vector3(x, y, z));
  public PrefabTemplate WithRotation(Quaternion rotation) { Rotation = rotation; return this; }
  public PrefabTemplate WithRotation(float x, float y, float z) => WithRotation(Quaternion.Euler(x, y, z));
  public PrefabTemplate WithScale(Vector3 scale) { Scale = scale; return this; }
  public PrefabTemplate WithScale(float scale) => WithScale(new Vector3(scale, scale, scale));
  public PrefabTemplate WithScale(float x, float y, float z) => WithScale(new Vector3(x, y, z));
  public override PrefabTemplate SetActive(bool active = true) { Template.SetActive(active); return this; }
}