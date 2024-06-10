using System.Linq;
using System.Text;
using Helpers;
using Track;
using UnityEngine;

namespace MapEditor.Helpers
{
  public sealed class TrackNodeHelper : MonoBehaviour, IPickable
  {

    private static readonly Material _LineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

    private TrackNode? _node;

    private TrackNode? Node => _node ??= transform.parent.GetComponent<TrackNode>();

    private LineRenderer? _LineRenderer;

    public float MaxPickDistance => 200f;

    public int Priority => 1;

    public TooltipInfo TooltipInfo => new TooltipInfo($"Node {Node?.id}", getTooltipText());

    private string getTooltipText()
    {
      var sb = new StringBuilder();
      sb.AppendLine($"ID: {Node.id}");
      sb.AppendLine($"Pos: {Node.transform.localPosition}");
      sb.AppendLine($"Rot: {Node.transform.localEulerAngles}");
      var segments = Graph.Shared.SegmentsConnectedTo(Node).Select(s => $"{s.id} ({(s.a == Node ? "A" : "B")})");
      sb.AppendLine($"Segments: {string.Join(", ", segments)}");
      return sb.ToString();
    }

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
      var cc = gameObject.AddComponent<BoxCollider>();
      cc.size = new Vector3(0.4f, 0.4f, 0.8f);
    }

    public void Update()
    {
      if (Node == null)
      {
        Destroy(this);
        return;
      }

      _LineRenderer.material.color = EditorContext.SelectedNode == Node ? Color.magenta : Color.cyan;
      _LineRenderer.enabled = EditorContext.Settings.ShowHelpers;

      if (!EditorMod.Shared.IsEnabled)
      {
        Destroy(this);
      }
    }

    public void Activate()
    {
      EditorContext.SelectedNode = Node;
    }

    public void Deactivate()
    {
    }

  }
}
