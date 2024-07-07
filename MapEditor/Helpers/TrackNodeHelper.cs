using System.Linq;
using System.Text;
using Helpers;
using JetBrains.Annotations;
using Track;
using UnityEngine;

namespace MapEditor.Helpers
{
  public sealed class TrackNodeHelper : MonoBehaviour, IPickable
  {

    private static readonly Material _LineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

    private LineRenderer? _LineRenderer;

    public float MaxPickDistance => 200f;

    public int Priority => 1;

    public TooltipInfo TooltipInfo => BuildTooltipInfo();

    private TooltipInfo BuildTooltipInfo()
    {
      var node = transform.parent.GetComponent<TrackNode>();
      if (node == null || EditorContext.PatchEditor == null)
      {
        return TooltipInfo.Empty;
      }

      var sb = new StringBuilder();
      sb.AppendLine($"ID: {node.id}");
      sb.AppendLine($"Pos: {node.transform.localPosition}");
      sb.AppendLine($"Rot: {node.transform.localEulerAngles}");
      var segments = Graph.Shared.SegmentsConnectedTo(node).Select(s => $"{s.id} ({(s.a == node ? "A" : "B")})");
      sb.AppendLine($"Segments: {string.Join(", ", segments)}");

      return new TooltipInfo($"Node {node.id}", sb.ToString());
    }

    [UsedImplicitly]
    public void Start()
    {
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
      var node = transform.parent.GetComponent<TrackNode>();
      if (EditorMod.Shared?.IsEnabled != true || node == null)
      {
        Destroy(this);
        return;
      }

      _LineRenderer!.material.color = EditorContext.SelectedNode == node ? Color.magenta : Color.cyan;
      _LineRenderer.enabled = EditorContext.PatchEditor != null;
    }

    public void Activate()
    {
      EditorContext.SelectedNode = transform.parent.GetComponent<TrackNode>();
    }

    public void Deactivate()
    {
    }

  }
}
