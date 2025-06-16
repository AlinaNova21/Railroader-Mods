using AlinasMapMod.MapEditor;
using Helpers;
using JetBrains.Annotations;
using RLD;
using UnityEngine;

namespace MapEditor.Helpers;

public sealed class ObjectHelper : MonoBehaviour, IPickable
{
  private static readonly Material _Material = new(Shader.Find("Universal Render Pipeline/Lit"));

  private IEditableObject _Object = null!;

  private GameObject? _Shape;
  public float MaxPickDistance => 200f;
  public int Priority => 1;
  public TooltipInfo TooltipInfo => BuildTooltipInfo();
  public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

  private TooltipInfo BuildTooltipInfo()
  {
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"Id: {_Object.Id}");
    if (_Object is ITransformableObject) {
      var transformable = (ITransformableObject)_Object;
      if (transformable.CanMove)
        sb.AppendLine($"Position: {transformable.Position}");
      if (transformable.CanRotate)
        sb.AppendLine($"Rotation: {transformable.Rotation}");
    }
    foreach (var property in _Object.Properties) {
      var val = _Object.GetProperty(property);
      if (val != null)
        sb.AppendLine($"{property}: {val}");
    }
    return new TooltipInfo($"{_Object.DisplayType} {_Object.Id}", sb.ToString());
  }

  [UsedImplicitly]
  public void Start()
  {
    _Object = transform.parent.GetComponent<IEditableObject>()!;

    transform.localPosition = Vector3.zero;
    transform.localEulerAngles = Vector3.zero;

    if (_Object is ICustomHelper customHelper) {
      _Shape = GameObject.Instantiate(customHelper.HelperPrefab, transform);
    } else {
      _Shape = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
      _Shape.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
      _Shape.transform.localPosition = new Vector3(0, 1f, 0);
      _Shape.transform.localEulerAngles = Vector3.zero;
    }
    _Shape.layer = Layers.Clickable;

    _Shape.transform.SetParent(transform, false);
    _Shape.GetMeshRenderer().material = _Material;
  }

  [UsedImplicitly]
  public void Update()
  {
    var mr = _Shape.GetMeshRenderer();
    mr.material.color = EditorContext.SelectedObjects.Contains(_Object) ? Color.magenta : Color.cyan;
    mr.enabled = EditorContext.PatchEditor != null;
  }

  public void Activate(PickableActivateEvent evt)
  {
    if (evt.Activation == PickableActivation.Secondary) {
      ShowContextMenu();
      return;
    }
    if (evt.IsControlDown) {
      if (EditorContext.SelectedObjects.Contains(_Object))
        EditorContext.RemoveSelectedObject(_Object);
      else 
        EditorContext.AddSelectedObject(_Object);
      return;
    }
    EditorContext.SelectedObject = _Object;
  }

  public void ShowContextMenu()
  {
    //var node = transform.parent.GetComponent<TrackNode>();
    //var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
    //var shared = UI.ContextMenu.ContextMenu.Shared;
    //if (UI.ContextMenu.ContextMenu.IsShown)
    //{
    //  shared.Hide();
    //}
    //shared.Clear();
    //shared.AddButton(ContextMenuQuadrant.General, (EditorContext.SelectedNode == node) ? "Deselect" : "Select", SpriteName.Select, () => EditorContext.SelectedNode = (EditorContext.SelectedNode == node) ? null : node);
    //shared.AddButton(ContextMenuQuadrant.Brakes, "Delete", sprite, () => NodeManager.RemoveNode(node));
    //shared.AddButton(ContextMenuQuadrant.Unused1, "Flip Switch Stand", sprite, () => NodeManager.FlipSwitchStand(!node.flipSwitchStand, node));
    //shared.AddButton(ContextMenuQuadrant.Unused2, "Split", sprite, () => NodeManager.SplitNode(node));

    //shared.Show($"Node {node.id}");
  }

  public void Deactivate()
  {
  }
}
