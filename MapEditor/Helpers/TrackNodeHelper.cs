using System.Linq;
using System.Text;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Dialogs;
using MapEditor.Managers;
using Serilog;
using Track;
using UI;
using UI.ContextMenu;
using UnityEngine;

namespace MapEditor.Helpers
{
  public sealed class TrackNodeHelper : MonoBehaviour, IPickable
  {
    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit"));

    private TrackNode _TrackNode = null!;

    private LineRenderer? _LineRenderer;

    public float MaxPickDistance => 200f;

    public int Priority => 1;

    public TooltipInfo TooltipInfo => BuildTooltipInfo();
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

    private TooltipInfo BuildTooltipInfo()
    {
      var sb = new StringBuilder();
      sb.AppendLine($"ID: {_TrackNode.id}");
      sb.AppendLine($"Pos: {_TrackNode.transform.localPosition}");
      sb.AppendLine($"Rot: {_TrackNode.transform.localEulerAngles}");
      var segments = Graph.Shared.SegmentsConnectedTo(_TrackNode).Select(s => $"{s.id} ({(s.a == _TrackNode ? "A" : "B")})");
      sb.AppendLine($"Segments: {string.Join(", ", segments)}");

      return new TooltipInfo($"Node {_TrackNode.id}", sb.ToString());
    }

    [UsedImplicitly]
    public void Start()
    {
      _TrackNode = transform.parent.GetComponent<TrackNode>()!;

      transform.localPosition = Vector3.zero;
      transform.localEulerAngles = Vector3.zero;

      gameObject.layer = Layers.Clickable;

      _LineRenderer = gameObject.AddComponent<LineRenderer>();
      _LineRenderer.material = _LineMaterial;
      _LineRenderer.startWidth = 0.05f;
      _LineRenderer.positionCount = 3;
      _LineRenderer.useWorldSpace = false;
      _LineRenderer.SetPosition(0, new Vector3(-0.2f, 0, -0.4f));
      _LineRenderer.SetPosition(1, new Vector3(0, 0, 0.6f));
      _LineRenderer.SetPosition(2, new Vector3(0.2f, 0, -0.4f));

      var boxCollider = gameObject.AddComponent<BoxCollider>();
      boxCollider.size = new Vector3(0.4f, 0.4f, 0.8f);
    }

    [UsedImplicitly]
    public void Update()
    {
      _LineRenderer!.material.color = EditorContext.SelectedNode == _TrackNode ? Color.magenta : Color.cyan;
      _LineRenderer.enabled = EditorContext.PatchEditor != null;
    }

    public void Activate(PickableActivateEvent evt)
    {
      if (evt.Activation == PickableActivation.Secondary)
      {
        ShowContextMenu();
        return;
      }
      EditorContext.SelectedNode = _TrackNode;
    }

    public void ShowContextMenu()
    {
      var node = transform.parent.GetComponent<TrackNode>();
      var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
      var shared = UI.ContextMenu.ContextMenu.Shared;
      if (UI.ContextMenu.ContextMenu.IsShown)
      {
        shared.Hide();
      }
      shared.Clear();
      shared.AddButton(ContextMenuQuadrant.General, (EditorContext.SelectedNode == node) ? "Deselect" : "Select", SpriteName.Select, () => EditorContext.SelectedNode = (EditorContext.SelectedNode == node) ? null : node);
      shared.AddButton(ContextMenuQuadrant.Brakes, "Delete", sprite, () => NodeManager.RemoveNode(node));
      shared.AddButton(ContextMenuQuadrant.Unused1, "Flip Switch Stand", sprite, () => NodeManager.FlipSwitchStand(!node.flipSwitchStand, node));
      shared.AddButton(ContextMenuQuadrant.Unused2, "Split", sprite, () => NodeManager.SplitNode(node));

      shared.Show($"Node {node.id}");
    }

    public void Deactivate()
    {
    }

  }
}
