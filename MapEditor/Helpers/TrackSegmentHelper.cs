using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Managers;
using Track;
using UI;
using UI.ContextMenu;
using UnityEngine;

namespace MapEditor.Helpers;

public sealed class TrackSegmentHelper : MonoBehaviour, IPickable
{

  private static readonly Color _Yellow = new(1, 1, 0);
  private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit"));
  private LineRenderer _LineRenderer = null!;
  private TrackSegment _Segment = null!;
  private BezierCurve _Curve;

  [UsedImplicitly]
  public void Start()
  {
    _Segment = transform.parent.GetComponent<TrackSegment>()!;
    transform.localPosition = Vector3.zero;
    transform.localEulerAngles = Vector3.zero;

    _LineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
    _LineRenderer.material = _LineMaterial;
    _LineRenderer.startWidth = 0.05f;
    _LineRenderer.endWidth = 0.05f;
    _LineRenderer.useWorldSpace = false;

    Rebuild();
  }

  public void Rebuild()
  {
    if (_Segment == null!) {
      return;
    }
    _Curve = _Segment.CreateBezier();
    var points = _Curve.Approximate().Select(p => p.point + new Vector3(0, 0.02f, 0)).ToArray();

    _LineRenderer.positionCount = points.Length;
    _LineRenderer.SetPositions(points);

    ReBuildChevrons();
  }

  [UsedImplicitly]
  public void Update()
  {
    _LineRenderer.material.color = EditorContext.SelectedSegment == _Segment ? Color.green : _Yellow;
    _LineRenderer.enabled = EditorContext.PatchEditor != null;

    UpdateChevrons();
  }


  public void Activate(PickableActivateEvent evt)
  {
    if (evt.Activation == PickableActivation.Secondary) {
      ShowContextMenu();
      return;
    }
    EditorContext.SelectedSegment = _Segment;
  }

  private void ShowContextMenu()
  {
    var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
    var shared = UI.ContextMenu.ContextMenu.Shared;
    if (UI.ContextMenu.ContextMenu.IsShown) {
      shared.Hide();
    }
    shared.Clear();
    shared.AddButton(ContextMenuQuadrant.General, (EditorContext.SelectedSegment == _Segment) ? "Deselect" : "Select", SpriteName.Select, () => EditorContext.SelectedSegment = (EditorContext.SelectedSegment == _Segment) ? null : _Segment);
    shared.AddButton(ContextMenuQuadrant.Brakes, "Delete", sprite, () => SegmentManager.RemoveSegment(_Segment));
    shared.AddButton(ContextMenuQuadrant.Unused1, "Inject Node", sprite, () => NodeManager.InjectNode(_Segment));
    shared.AddButton(ContextMenuQuadrant.Unused2, "Track Style", sprite, () => StartCoroutine(ShowTrackStyleContextMenu()));

    shared.Show($"Segment {_Segment.id}");
  }

  private IEnumerator ShowTrackStyleContextMenu()
  {
    yield return new WaitForSeconds(0.1f);
    var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
    var shared = UI.ContextMenu.ContextMenu.Shared;
    if (UI.ContextMenu.ContextMenu.IsShown) {
      shared.Hide();
    }
    shared.Clear();

    shared.AddButton(ContextMenuQuadrant.General, "Standard", sprite, () => SegmentManager.UpdateStyle(TrackSegment.Style.Standard, _Segment));
    shared.AddButton(ContextMenuQuadrant.Brakes, "Yard", sprite, () => SegmentManager.UpdateStyle(TrackSegment.Style.Yard, _Segment));
    shared.AddButton(ContextMenuQuadrant.Unused1, "Tunnel", sprite, () => SegmentManager.UpdateStyle(TrackSegment.Style.Tunnel, _Segment));
    shared.AddButton(ContextMenuQuadrant.Unused2, "Bridge", sprite, () => SegmentManager.UpdateStyle(TrackSegment.Style.Bridge, _Segment));
    shared.Show($"Segment {_Segment.id}\nTrack Style");
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
    var sb = new StringBuilder();
    sb.AppendLine($"ID: {_Segment.id}");
    sb.AppendLine($"A: {_Segment.a.id}");
    sb.AppendLine($"B: {_Segment.b.id}");
    sb.AppendLine($"Priority: {_Segment.priority}");
    sb.AppendLine($"Speed: {_Segment.speedLimit}");
    sb.AppendLine($"GroupId: {_Segment.groupId}");
    sb.AppendLine($"Style: {_Segment.style}");
    sb.AppendLine($"Class: {_Segment.trackClass}");
    sb.AppendLine($"Length: {_Curve.CalculateLength()}m");


    return new TooltipInfo($"Segment {_Segment.id}", sb.ToString());
  }

  #region Chevrons

  private readonly List<LineRenderer> _Chevrons = new();

  private void UpdateChevrons()
  {
    foreach (var lineRenderer in _Chevrons) {
      lineRenderer.material.color = EditorContext.SelectedSegment == _Segment ? Color.green : _Yellow;
    }
  }

  private void ReBuildChevrons()
  {
    foreach (var chevron in _Chevrons) {
      Destroy(chevron.gameObject);
    }

    _Chevrons.Clear();

    const float carLength = 12.2f;

    var length = _Segment.GetLength();

    var chevronCount = Mathf.Floor(length / carLength);

    if (chevronCount == 0) {
      // segment too short - render single chevron in center
      _Chevrons.Add(CreateChevron(0.5f));
    } else {
      // render chevrons centered on segment
      var firstOffset = (length - chevronCount * carLength) * 0.5f / length;
      if (firstOffset < carLength / 2) {
        // gap between first chevron and node is too small
        --chevronCount;
        firstOffset = (length - chevronCount * carLength) * 0.5f / length;
      }

      var deltaT = carLength / length;
      for (var t = firstOffset; t < 1; t += deltaT) {
        _Chevrons.Add(CreateChevron(t));
      }
    }
  }

  private LineRenderer CreateChevron(float t)
  {
    var chevron = new GameObject("TrackSegmentHelper_Chevron");
    chevron.transform.parent = transform;
    chevron.transform.localPosition = _Curve.GetPoint(t) + new Vector3(0, 0.025f, 0);
    chevron.transform.localEulerAngles = _Curve.GetRotation(t).eulerAngles;
    chevron.layer = Layers.Clickable;

    var lineRenderer = chevron.AddComponent<LineRenderer>();
    lineRenderer.material = _LineMaterial;
    lineRenderer.startWidth = 0.05f;
    lineRenderer.positionCount = 3;
    lineRenderer.useWorldSpace = false;
    lineRenderer.SetPosition(0, new Vector3(-0.15f, 0, -0.3f));
    lineRenderer.SetPosition(1, new Vector3(0, 0, 0.45f));
    lineRenderer.SetPosition(2, new Vector3(0.15f, 0, -0.3f));
    _Chevrons.Add(lineRenderer);

    var boxCollider = chevron.AddComponent<BoxCollider>();
    boxCollider.size = new Vector3(0.4f, 0.4f, 0.8f);

    return lineRenderer;
  }

  #endregion

}
