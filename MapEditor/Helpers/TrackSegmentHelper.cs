using System.Linq;
using Helpers;
using JetBrains.Annotations;
using Track;
using UnityEngine;

namespace MapEditor.Helpers
{
  public sealed class TrackSegmentHelper : MonoBehaviour
  {

    private static readonly Material _LineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

    private LineRenderer? _LineRenderer;
    
    [UsedImplicitly]
    public void Start()
    {
      transform.localPosition = Vector3.zero;
      transform.localEulerAngles = Vector3.zero;

      gameObject.layer = Layers.Clickable;

      Rebuild();
    }

    public void Rebuild()
    {
      var segment = transform.parent.GetComponent<TrackSegment>();
      if (segment == null)
      {
        return;
      }

      var approx = segment.Curve.Approximate();
      var points = approx.Select(p => p.point + new Vector3(0, -0.02f, 0)).ToArray();

      _LineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
      _LineRenderer.material = _LineMaterial;
      _LineRenderer.startWidth = 0.1f;
      _LineRenderer.endWidth = 0.1f;
      _LineRenderer.useWorldSpace = false;
      _LineRenderer.positionCount = points.Length;
      _LineRenderer.SetPositions(points);
    }

    [UsedImplicitly]
    public void Update()
    {
      var segment = transform.parent.GetComponent<TrackSegment>();
      if (EditorMod.Shared?.IsEnabled != true || segment == null)
      {
        Destroy(this);
      }

      _LineRenderer!.material.color = EditorContext.SelectedSegment == segment ? Color.green : Color.yellow;
      _LineRenderer.enabled = EditorContext.Settings.ShowHelpers;
    }

  }
}
