using System;
using System.Linq;
using System.Text;
using Track;
using UnityEngine;

namespace MapEditor
{
  class TrackNodeHelper : MonoBehaviour, IPickable
  {
    private static Material _lineMaterial;
    private static Material LineMaterial
    {
      get
      {
        if (_lineMaterial == null)
        {
          _lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
          {
            color = Color.cyan
          };
        }
        return _lineMaterial;
      }
    }

    private TrackNode _node;
    private TrackNode Node
    {
      get
      {
        if (_node == null)
        {
          _node = transform.parent.GetComponent<TrackNode>();
        }
        return _node;
      }
    }

    private LineRenderer LineRenderer;

    public float MaxPickDistance => 200f;

    public int Priority => 1;

    public float pointAvg = 0;

    public TooltipInfo TooltipInfo => new TooltipInfo($"Node {Node.id}", this.getTooltipText());

    private string getTooltipText()
    {
      var sb = new StringBuilder();
      sb.AppendLine($"ID: {Node.id}");
      sb.AppendLine($"Pos: {Node.transform.localPosition}");
      sb.AppendLine($"Rot: {Node.transform.localEulerAngles}");
      var segments = Graph.Shared.SegmentsConnectedTo(Node).Select(s => $"{s.id} ({(s.a == Node ? "A" : "B")})");
      sb.AppendLine($"Segments: {String.Join(", ", segments)}");
      return sb.ToString();
    }

    public void Start()
    {
      gameObject.layer = LayerMask.NameToLayer("Clickable");
      LineRenderer = gameObject.AddComponent<LineRenderer>();
      LineRenderer.material = LineMaterial;
      LineRenderer.startWidth = 0.05f;
      LineRenderer.positionCount = 3;
      LineRenderer.useWorldSpace = false;
      LineRenderer.SetPosition(0, new Vector3(-0.2f, 0, -0.4f));
      LineRenderer.SetPosition(1, new Vector3(0, 0, 0.6f));
      LineRenderer.SetPosition(2, new Vector3(0.2f, 0, -0.4f));
      var cc = gameObject.AddComponent<BoxCollider>();
      cc.size = new Vector3(0.4f, 0.4f, 0.8f);
    }

    public void Update()
    {
      LineRenderer.enabled = EditorMod.Shared.Settings.ShowHelpers;

      if (!EditorMod.Shared.IsEnabled)
      {
        Destroy(this);
      }
    }

    public void Activate()
    {
    }

    public void Deactivate()
    {
    }
  }
}
