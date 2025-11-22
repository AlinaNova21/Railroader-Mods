using AlinasMapMod.Caches;
using AlinasMapMod.Validation;
using Helpers;
using StrangeCustoms.Tracks;
using UI.Map;
using UnityEngine;

namespace AlinasMapMod.Definitions;

[RootObject("mapLabels")]
public class SerializedMapLabel : SerializedComponentBase<MapLabel>,
  ICreatableComponent<MapLabel>,
  IDestroyableComponent<MapLabel>
{
  public SerializedVector3 Position { get; set; } = new SerializedVector3();
  public string Text { get; set; } = "Map Label";

  protected override void ConfigureValidation()
  {
    RuleFor(() => Text)
      .Required()
      .Custom((text, context) =>
      {
        var result = new ValidationResult { IsValid = true };
        if (string.IsNullOrWhiteSpace(text))
        {
          result.IsValid = false;
          result.Errors.Add(new ValidationError
          {
            Field = nameof(Text),
            Message = "Text cannot be empty or whitespace",
            Code = "TEXT_REQUIRED",
            Value = text
          });
        }
        return result;
      });
      
    RuleFor(() => Position).Required();
  }

  public override MapLabel Create(string id)
  {
    GameObject go = null;
    try
    {
      // Find the Map Labels parent
      var parent = GameObject.Find("Map Labels");
      if (parent == null)
      {
        throw new System.InvalidOperationException("Map Labels parent not found");
      }

      go = new GameObject(id);
      go.transform.parent = parent.transform;
      go.layer = Layers.Map;
      
      // Clone an existing MapLabel to get the proper setup
      var templateMapLabel = parent.GetComponentInChildren<MapLabel>();
      if (templateMapLabel == null)
      {
        throw new System.InvalidOperationException("No template MapLabel found");
      }
      
      var clone = Object.Instantiate(templateMapLabel.gameObject, go.transform, true);
      clone.name = "MapLabel";
      clone.transform.localPosition = Vector3.zero;
      
      var comp = clone.GetComponent<MapLabel>();
      comp.name = id;
      
      // Set up the private _canvas field using reflection
      var canvas = clone.GetComponent<Canvas>();
      typeof(MapLabel)
        .GetField("_canvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
        .SetValue(comp, canvas);
      
      Write(comp);
      MapLabelCache.Instance[id] = comp;
      return comp;
    }
    catch
    {
      if (go != null) Object.DestroyImmediate(go);
      MapLabelCache.Instance.Remove(id);
      throw;
    }
  }

  public void Destroy(MapLabel comp)
  {
    GameObject.Destroy(comp.transform.parent.gameObject); // Destroy the parent GameObject
    MapLabelCache.Instance.Remove(comp.name);
  }

  public override void Read(MapLabel comp)
  {
    Position = comp.transform.parent.localPosition;
    Text = comp.text;
  }

  public override void Write(MapLabel comp)
  {
    comp.transform.parent.localPosition = Position;
    comp.text = Text;
    
    // Update the TextMeshProUGUI component as well
    var textComponent = comp.GetComponentInChildren<TMPro.TextMeshProUGUI>();
    if (textComponent != null)
    {
      textComponent.text = Text;
    }
  }
}