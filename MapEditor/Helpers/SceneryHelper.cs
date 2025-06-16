using System.Text;
using Helpers;
using JetBrains.Annotations;
using RLD;
using UnityEngine;

namespace MapEditor.Helpers;

public sealed class SceneryHelper : MonoBehaviour, IPickable
{
  private static readonly Material _Material = new(Shader.Find("Universal Render Pipeline/Lit"));

  private SceneryAssetInstance _Scenery = null!;

  private GameObject? _Sphere;

  public float MaxPickDistance => 200f;

  public int Priority => 1;

  public TooltipInfo TooltipInfo => BuildTooltipInfo();
  public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

  private TooltipInfo BuildTooltipInfo()
  {
    var sb = new StringBuilder();
    sb.AppendLine($"ID: {_Scenery.name}");
    sb.AppendLine($"Pos: {_Scenery.transform.localPosition}");
    sb.AppendLine($"Rot: {_Scenery.transform.localEulerAngles}");
    sb.AppendLine($"Model: {_Scenery.identifier}");

    return new TooltipInfo($"Scenery {_Scenery.name}", sb.ToString());
  }

  [UsedImplicitly]
  public void Start()
  {
    _Scenery = transform.parent.GetComponent<SceneryAssetInstance>()!;

    transform.localPosition = Vector3.zero;
    transform.localEulerAngles = Vector3.zero;

    _Sphere = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    _Sphere.layer = Layers.Clickable;
    _Sphere.transform.parent = transform;
    _Sphere.transform.localPosition = new Vector3(0, 1f, 0);
    _Sphere.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
    _Sphere.transform.localEulerAngles = Vector3.zero;
    _Sphere.GetMeshRenderer().material = _Material;
  }

  [UsedImplicitly]
  public void Update()
  {
    var mr = _Sphere.GetMeshRenderer();
    mr.material.color = EditorContext.SelectedScenery == _Scenery ? Color.magenta : Color.cyan;
    mr.enabled = EditorContext.PatchEditor != null;
  }

  public void Activate(PickableActivateEvent evt)
  {
    if (evt.Activation == PickableActivation.Secondary) {
      ShowContextMenu();
      return;
    }
    EditorContext.SelectedScenery = _Scenery;
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
