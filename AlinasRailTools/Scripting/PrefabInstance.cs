using UnityEngine;
using Track;

namespace AlinasRailTools.Scripting;

public class PrefabInstance : BaseBuilder<PrefabBuilder, PrefabInstance>
{
  public GameObject Instance { get; private set; }
  public Helper Helper => Parent.Helper;

  public PrefabInstance(PrefabBuilder parent, GameObject instance)
    : base(parent, instance.name) => Instance = instance;

  public override PrefabBuilder Remove() => Parent.RemoveInstance(Id);

  public override PrefabInstance Invalidate() => this; // Instances don't need invalidation

  // Properties for GameObject
  public string Name { get => Instance.name; set { Instance.name = value; } }
  public Vector3 Position { get => Instance.transform.position; set { Instance.transform.position = value; } }
  public Quaternion Rotation { get => Instance.transform.rotation; set { Instance.transform.rotation = value; } }
  public Vector3 Scale { get => Instance.transform.localScale; set { Instance.transform.localScale = value; } }
  public Transform ParentObject { get => Instance.transform.parent; set { Instance.transform.SetParent(value); } }

  // Template assignment
  public PrefabInstance WithTemplate(string templateId)
  {
    if (!PrefabBuilder.Templates.ContainsKey(templateId))
    {
      throw new System.Exception($"Template with id {templateId} does not exist");
    }

    var template = PrefabBuilder.Templates[templateId];
    return WithTemplate(template);
  }

  public PrefabInstance WithTemplate(PrefabTemplate templateBuilder)
  {
    return WithTemplate(templateBuilder.Template);
  }

  public PrefabInstance WithTemplate(GameObject template)
  {
    // Create instance from template
    var newInstance = Object.Instantiate(template);
    newInstance.name = Instance.name; // Keep the instance name
    newInstance.SetActive(false); // Start inactive, user will activate with SetActive()

    // Replace the old instance
    if (Instance != newInstance)
    {
      Object.DestroyImmediate(Instance);
    }

    Instance = newInstance;

    // Update cache
    PrefabBuilder.AllInstances[Id] = newInstance;

    return this;
  }

  // Track-relative positioning
  public PrefabInstance PlaceNextToTrack(Location location, bool flip, float distance)
  {
    // Get position and rotation from track location
    var trackPosition = location.GetPosition();
    var trackRotation = location.GetRotation();
    var trackDirection = trackRotation * Vector3.forward;

    // Calculate perpendicular direction (left side of track)
    var perpendicular = Vector3.Cross(trackDirection, Vector3.up).normalized;

    // Flip to right side if requested
    if (flip)
    {
      perpendicular = -perpendicular;
    }

    // Calculate final position
    var finalPosition = trackPosition + (perpendicular * distance);

    // Set position and rotation to face the track
    Position = finalPosition;
    Rotation = trackRotation;

    return this;
  }

  // Fluent methods
  public PrefabInstance WithName(string name) { Name = name; return this; }
  public PrefabInstance At(Vector3 position) { Position = position; return this; }
  public PrefabInstance At(float x, float y, float z) => At(new Vector3(x, y, z));
  public PrefabInstance WithRotation(Quaternion rotation) { Rotation = rotation; return this; }
  public PrefabInstance WithRotation(float x, float y, float z) => WithRotation(Quaternion.Euler(x, y, z));
  public PrefabInstance WithScale(Vector3 scale) { Scale = scale; return this; }
  public PrefabInstance WithScale(float scale) => WithScale(new Vector3(scale, scale, scale));
  public PrefabInstance WithScale(float x, float y, float z) => WithScale(new Vector3(x, y, z));
  public PrefabInstance WithParentObject(Transform parent) { ParentObject = parent; return this; }
  public override PrefabInstance SetActive(bool active = true) { Instance.SetActive(active); return this; }
}