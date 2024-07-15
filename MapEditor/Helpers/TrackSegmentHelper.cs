using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers;
using JetBrains.Annotations;
using Track;
using UnityEngine;

namespace MapEditor.Helpers;

public sealed class TrackSegmentHelper : MonoBehaviour, IPickable
{

  private static readonly Color _Yellow = new(1, 1, 0);
  private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit"));
  private readonly List<LineRenderer> _LineRenderers = new();

  private TrackSegment? _Segment;

  [UsedImplicitly]
  public void Start()
  {
    Rebuild();
  }

  public void Rebuild()
  {
    _Segment = transform.parent.GetComponent<TrackSegment>();
    if (_Segment == null)
    {
      return;
    }

    transform.localPosition = Vector3.zero;
    transform.localEulerAngles = Vector3.zero;

    var points = _Segment.Curve.Approximate().Select(p => p.point + new Vector3(0, 0.02f, 0)).ToArray();

    var mainLineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
    mainLineRenderer.material = _LineMaterial;
    mainLineRenderer.startWidth = 0.05f;
    mainLineRenderer.endWidth = 0.05f;
    mainLineRenderer.useWorldSpace = false;
    mainLineRenderer.positionCount = points.Length;
    mainLineRenderer.SetPositions(points);
    _LineRenderers.Add(mainLineRenderer);

    for (var t = 0.1f; t < 1; t += 0.1f)
    {
      _LineRenderers.Add(CreateChevron(t));
    }
  }

  private LineRenderer CreateChevron(float t)
  {
    var chevron = new GameObject("TrackSegmentHelper_Chevron");
    chevron.transform.parent = transform;
    chevron.transform.localPosition = _Segment!.Curve.GetPoint(t) + new Vector3(0, 0.025f, 0);
    chevron.transform.localEulerAngles = _Segment.Curve.GetRotation(t).eulerAngles;
    chevron.layer = Layers.Clickable;

    var lineRenderer = chevron.AddComponent<LineRenderer>();
    lineRenderer.material = _LineMaterial;
    lineRenderer.startWidth = 0.05f;
    lineRenderer.positionCount = 3;
    lineRenderer.useWorldSpace = false;
    lineRenderer.SetPosition(0, new Vector3(-0.1f, 0, -0.2f));
    lineRenderer.SetPosition(1, new Vector3(0, 0, 0.3f));
    lineRenderer.SetPosition(2, new Vector3(0.1f, 0, -0.2f));
    _LineRenderers.Add(lineRenderer);

    var boxCollider = chevron.AddComponent<BoxCollider>();
    boxCollider.size = new Vector3(0.4f, 0.4f, 0.8f);

    return lineRenderer;
  }

  [UsedImplicitly]
  public void Update()
  {
    var segment = transform.parent.GetComponent<TrackSegment>();
    if (EditorMod.Shared?.IsEnabled != true || segment == null)
    {
      Destroy(this);
    }

    foreach (var lineRenderer in _LineRenderers)
    {
      lineRenderer.material.color = EditorContext.SelectedSegment == segment ? Color.green : _Yellow;
      lineRenderer.enabled = EditorContext.PatchEditor != null;
    }
  }

  public void Activate(PickableActivateEvent evt)
  {
    EditorContext.SelectedSegment = transform.parent.GetComponent<TrackSegment>();
  }

  public void Deactivate()
  {
  }

  public float MaxPickDistance => 200;

  public int Priority => -1;

  public TooltipInfo TooltipInfo => BuildTooltipInfo();

  public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

  private TooltipInfo BuildTooltipInfo()
  {
    var segment = transform.parent.GetComponent<TrackSegment>();
    if (segment == null || EditorContext.PatchEditor == null)
    {
      return TooltipInfo.Empty;
    }

    var sb = new StringBuilder();
    sb.AppendLine($"ID: {segment.id}");
    sb.AppendLine($"A: {segment.a?.id}");
    sb.AppendLine($"B: {segment.b?.id}");
    sb.AppendLine($"Priority: {segment.priority}");
    sb.AppendLine($"Speed: {segment.speedLimit}");
    sb.AppendLine($"GroupId: {segment.groupId}");
    sb.AppendLine($"Style: {segment.style}");
    sb.AppendLine($"Class: {segment.trackClass}");
    sb.AppendLine($"Length: {segment.Curve.CalculateLength()}m");
    

    return new TooltipInfo($"Segment {segment.id}", sb.ToString());
  }

}
