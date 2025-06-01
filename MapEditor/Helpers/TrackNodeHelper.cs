using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Managers;
using MapEditor.StateTracker.Node;
using Serilog;
using Track;
using UI;
using UI.ContextMenu;
using UnityEngine;

namespace MapEditor.Helpers;

public sealed class TrackNodeHelper : MonoBehaviour, IPickable
{
  private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit"));

  private static readonly Serilog.ILogger logger = Log.ForContext<TrackNodeHelper>();

  private TrackNode _TrackNode = null!;
  private TrackRebuilder? _TrackRebuilder;
  private LineRenderer? _LineRenderer;

  private LineRenderer[] _SwitchRenderers = [];
  private Camera? _camera;

  private bool _active = false;
  private float _activateTime = 0;
  private float _lastRebuild = 0;
  private bool _terrainHit = false;
  private Vector3 _lastPos = Vector3.zero;
  private Vector3 _lastRot = Vector3.zero;

  private List<TrackObjectManager.ITrackDescriptor> _ghostSegments = [];

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
    sb.AppendLine($"Active: {_active} ({(_active ? Time.time - _activateTime : 0):F2})");
    sb.AppendLine($"Terrain Hit: {_terrainHit}");
    sb.AppendLine($"Rebuild: {Time.time - _lastRebuild:F2} (Ghosts: {_ghostSegments.Count()})");
    return new TooltipInfo($"Node {_TrackNode.id}", sb.ToString());
  }

  [UsedImplicitly]
  public void Start()
  {
    _TrackNode = transform.parent.GetComponent<TrackNode>()!;
    _TrackRebuilder = TrackObjectManager.Instance.GetComponent<TrackRebuilder>();
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

    _SwitchRenderers = new LineRenderer[4];
    for (var i = 0; i < 4; i++) {
      var ngo = new GameObject($"SwitchLine{i}");
      ngo.transform.parent = transform;
      ngo.transform.localPosition = Vector3.zero;
      ngo.transform.localEulerAngles = Vector3.zero;
      var sr = ngo.AddComponent<LineRenderer>();
      sr.material = _LineMaterial;
      sr.startWidth = 0.05f;
      sr.positionCount = 0;
      sr.useWorldSpace = false;
      _SwitchRenderers[i] = sr;
      //sr.enabled = false;
    }

    var boxCollider = gameObject.AddComponent<BoxCollider>();
    boxCollider.size = new Vector3(0.4f, 0.4f, 0.8f);
  }

  [UsedImplicitly]
  public void Update()
  {
    _LineRenderer!.material.color = EditorContext.SelectedNode == _TrackNode ? Color.magenta : Color.cyan;
    _LineRenderer.enabled = EditorContext.PatchEditor != null;
    _terrainHit = false;
    if (_active && Time.time - _activateTime > 0.5f) {
      MainCameraHelper.TryGetIfNeeded(ref _camera);
      var cpos = _TrackNode.transform.localPosition;
      var ray = _camera.ScreenPointToRay(Input.mousePosition);
      Vector3 pos = _TrackNode.transform.localPosition;
      if (Physics.Raycast(ray, out var raycastHit, 1000)) {
        var obj = raycastHit.collider.gameObject;
        //if (!obj.name.Contains("Terrain")) continue;
        _terrainHit = true;
        var mpos = raycastHit.point;
        mpos = WorldTransformer.WorldToGame(mpos);
        logger.Information("Hit {obj} {pos} {point}", obj.name, mpos, raycastHit.point);
        //pos = new Vector3(mpos.x, pos.y, mpos.z);
        pos = mpos + (Vector3.up * 0.2f);
        _lastPos = pos;
        _lastRot = _TrackNode.transform.localEulerAngles;

        //EditorContext.ChangeManager.AddChange(new ChangeTrackNode(_TrackNode).Move(npos));
      }

      var rebuild = Time.time - _lastRebuild > 0.25f;
      if (rebuild) {
        var isNew = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        _lastRebuild = Time.time;
        var buildObject = typeof(TrackObjectManager).GetMethod("BuildGameObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        clearGhostSegments();

        var ghostObj = new GameObject($"Ghost {_TrackNode.id}");
        ghostObj.transform.parent = transform.parent;
        ghostObj.transform.localPosition = pos;
        ghostObj.transform.localEulerAngles = _TrackNode.transform.localEulerAngles;
        var node = ghostObj.AddComponent<TrackNode>();
        node.id = "ghost";

        if (isNew) {
          addGhostSegment(_TrackNode.transform, ghostObj.transform);
        } else {
          var segments = Graph.Shared.SegmentsConnectedTo(_TrackNode);
          foreach (var segment in segments) {
            var a = segment.GetOtherNode(_TrackNode);
            var b = ghostObj;
            addGhostSegment(a.transform, b.transform);
          }
        }
        Destroy(ghostObj);
      }
      //EditorContext.ChangeManager.AddChange()
    }
    if (!_active && _ghostSegments.Count() > 0) {
      clearGhostSegments();
    }
  }

  private void addGhostSegment(Transform a, Transform b)
  {
    Vector3 localPosition = a.transform.localPosition;
    Vector3 localPosition2 = b.transform.localPosition;
    float num = (localPosition - localPosition2).magnitude * TrackSegment.BezierTangentFactorForTangents(a.transform.forward, b.transform.forward);
    Vector3 vector = TangentPoint(a, b, num);
    Vector3 vector2 = TangentPoint(b, a, num);
    var curve = new BezierCurve([localPosition, vector, vector2, localPosition2], a.transform.up, b.transform.up);
    var segment = new TrackSegment
    {
      id = "ghost",
      style = TrackSegment.Style.Standard,
      a = a.gameObject.GetComponent<TrackNode>(),
      b = b.gameObject.GetComponent<TrackNode>()
    };
    var segmentProxy = new SegmentProxy(segment)
    {
      Curve = curve
    };
    var desc = new GhostSegmentDescriptor(segmentProxy);
    _TrackRebuilder!.Add(desc, curve);
    _ghostSegments.Add(desc);
  }

  private Vector3 TangentPoint(Transform a, Transform b, float d)
  {
    var Transform = (Vector3 v) => (a.localRotation * v) + a.localPosition;
    Vector3 vector = Transform(Vector3.forward);
    Vector3 vector2 = Transform(Vector3.back);
    float magnitude = (vector - b.transform.localPosition).magnitude;
    float magnitude2 = (vector2 - b.transform.localPosition).magnitude;
    float num = (magnitude < magnitude2) ? d : (-d);
    return Transform(Vector3.forward * num);
  }

  private void clearGhostSegments()
  {
    _ghostSegments.ForEach(_TrackRebuilder!.Remove);
    _ghostSegments.Clear();
  }

  private readonly struct GhostSegmentDescriptor : TrackObjectManager.ITrackDescriptor
  {
    // Token: 0x17000277 RID: 631
    // (get) Token: 0x0600160F RID: 5647 RVA: 0x000700E3 File Offset: 0x0006E2E3
    public string Identifier { get; }

    // Token: 0x06001610 RID: 5648 RVA: 0x000700EC File Offset: 0x0006E2EC
    public GhostSegmentDescriptor(SegmentProxy segment)
    {
      this.segment = segment;
      this.Identifier = string.Format("segment-ghost-{0}-{1:F1}-{2:F1}", segment.Segment.id, segment.Curve.EndPoint1, segment.Curve.EndPoint2);
    }

    // Token: 0x06001611 RID: 5649 RVA: 0x0007013D File Offset: 0x0006E33D
    public GameObject BuildGameObject(TrackObjectBuilder builder)
    {
      return builder.CreateSegmentObject(this.segment);
    }

    // Token: 0x06001612 RID: 5650 RVA: 0x0007014B File Offset: 0x0006E34B
    public GameObject BuildMaskObject(TrackObjectBuilder builder)
    {
      return new GameObject();
    }

    // Token: 0x06001613 RID: 5651 RVA: 0x00070159 File Offset: 0x0006E359
    public override string ToString()
    {
      return this.Identifier;
    }

    // Token: 0x040011D6 RID: 4566
    public readonly SegmentProxy segment;
  }
  public void SwitchHelper()
  {
    logger.Debug("SwitchHelper called {0}", _TrackNode.id);
    var graph = Graph.Shared;
    if (!graph.IsSwitch(_TrackNode)) {
      logger.Debug("Switch node {0} ({1}) is not a switch", _TrackNode.id, _TrackNode.transform.localPosition);
      foreach (var i in _SwitchRenderers) {
        i.positionCount = 0;
      };
    }
    Vector3 nodePosition = _TrackNode.transform.localPosition;
    TrackSegment trackSegment;
    TrackSegment segmentA;
    TrackSegment segmentB;
    if (graph.DecodeSwitchAt(_TrackNode, out trackSegment, out segmentA, out segmentB)) {
      logger.Debug("Switch node {0} ({1}) {2} {3}", _TrackNode.id, _TrackNode.transform.localPosition, segmentA.id, segmentB.id);
      var aProxy = new SegmentProxy(segmentA);
      var bProxy = new SegmentProxy(segmentB);
      BezierCurve bezierCurve;
      BezierCurve bezierCurve2;
      Vector3 vector;
      SwitchGeometryProxy.AlignSwitchCurves(aProxy, bProxy, out vector, out bezierCurve, out bezierCurve2);
      Gauge standard = Gauge.Standard;
      LinePoint linePoint;
      SwitchGeometry.RailLineCurves railLineCurves = SwitchGeometry.MakeTrackLineSegments(bezierCurve, standard);
      SwitchGeometry.RailLineCurves railLineCurves2 = SwitchGeometry.MakeTrackLineSegments(bezierCurve2, standard);
      var valid = SwitchGeometryProxy.Intersects(railLineCurves.left, railLineCurves2.right, 1.5f, out linePoint)
        || SwitchGeometryProxy.Intersects(railLineCurves.right, railLineCurves2.left, 1.5f, out linePoint);
      logger.Debug("Switch node {0} validity: {1}", _TrackNode.id, valid);

      LineCurve[] lines = [railLineCurves.left, railLineCurves2.right, railLineCurves.right, railLineCurves2.left];
      for (var i = 0; i < lines.Length; i++) {
        var line = lines[i];
        var lineRenderer = _SwitchRenderers[i];
        lineRenderer.enabled = true;
        lineRenderer.material.color = valid ? Color.green : Color.red;
        lineRenderer.positionCount = line.Points.Count();
        lineRenderer.transform.eulerAngles = Vector3.zero;
        var points = line.Points.ToArray();
        for (var j = 0; j < line.Points.Count(); j++) {
          var point = points[j];
          lineRenderer.SetPosition(j, point.point);
        }
      }
    }
  }

  public void Activate(PickableActivateEvent evt)
  {
    if (evt.Activation == PickableActivation.Secondary) {
      ShowContextMenu();
      return;
    }
    EditorContext.SelectedNode = _TrackNode;
    _active = true;
    _activateTime = Time.time;
  }

  public void ShowContextMenu()
  {
    var node = transform.parent.GetComponent<TrackNode>();
    var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
    var shared = UI.ContextMenu.ContextMenu.Shared;
    if (UI.ContextMenu.ContextMenu.IsShown) {
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
    _active = false;
    if (Time.time - _activateTime > 1f) {
      var isNew = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
      if (isNew) {
        var nid = EditorContext.TrackNodeIdGenerator.Next()!;
        //var sid = EditorContext.TrackSegmentIdGenerator.Next()!;
        EditorContext.ChangeManager.AddChange(new CreateTrackNode(nid, _lastPos, _lastRot));
        //EditorContext.ChangeManager.AddChange(new CompoundChange(
        //new CreateTrackNode(nid, _lastPos, _TrackNode.transform.localEulerAngles),
        //new CreateTrackSegment(sid, _TrackNode.id, nid)
        //));
        var newNode = Graph.Shared.GetNode(nid);
        EditorContext.SelectedNode = newNode;
        //var newSegment = Graph.Shared.GetSegment(sid);
        //EditorContext.SelectedSegment = newSegment;
      }
    }
    clearGhostSegments();
  }
}
