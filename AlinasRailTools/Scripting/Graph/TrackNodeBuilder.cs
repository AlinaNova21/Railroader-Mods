using System;
using System.Collections.Generic;
using Track;
using UnityEngine;

namespace AlinasRailTools.Scripting.Graph;

public class TrackNodeBuilder: BaseBuilder<GraphBuilder, TrackNodeBuilder>
{
  public TrackNode Node { get; }
  public Helper Helper => Parent.Helper;
  public TrackNodeBuilder(GraphBuilder graphBuilder, TrackNode node) : base(graphBuilder, node.id) => Node = node;

  public override GraphBuilder Remove() => Parent.RemoveTrackNode(Id);

  private static readonly HashSet<TrackNode> _pendingInvalidations = new();
  private static bool _batchMode = false;

  public static void BeginBatch() => _batchMode = true;
  public static void EndBatch()
  {
    _batchMode = false;
    foreach (var node in _pendingInvalidations)
    {
      try
      {
        Track.Graph.Shared.OnNodeDidChange(node);
      }
      catch (System.InvalidOperationException)
      {
        // Skip if collection is still being modified
      }
    }
    _pendingInvalidations.Clear();
  }

  public override TrackNodeBuilder Invalidate()
  {
    if (_batchMode)
    {
      _pendingInvalidations.Add(Node);
    }
    else
    {
      try
      {
        Track.Graph.Shared.OnNodeDidChange(Node);
      }
      catch (System.InvalidOperationException)
      {
        // Defer if collection is being modified
        _pendingInvalidations.Add(Node);
      }
    }
    return this;
  }
  public Vector3 Position { get => Node.transform.localPosition; set { Node.transform.localPosition = value; Invalidate(); } }
  public Quaternion Rotation { get => Node.transform.localRotation; set { Node.transform.localRotation = value; Invalidate(); } }
  public Vector3 Euler { get => Node.transform.localEulerAngles; set { Node.transform.localEulerAngles = value; Invalidate(); } }
  public Vector3 EulerAngles { get => Node.transform.localEulerAngles; set { Node.transform.localEulerAngles = value; Invalidate(); } }
  public bool FlipSwitchStand { get => Node.flipSwitchStand; set { Node.flipSwitchStand = value; Invalidate(); } }
  public Turntable Turntable { get => Node.turntable; set { Node.turntable = value; Invalidate(); } }

  public TrackNodeBuilder At(Vector3 position) { Position = position; return this; }
  public TrackNodeBuilder At(float x, float y, float z) => At(new Vector3(x, y, z));
  public TrackNodeBuilder WithRotation(Vector3 rotation) { Euler = rotation; return this; }
  public TrackNodeBuilder WithRotation(float x, float y, float z) => WithRotation(new Vector3(x, y, z));
  public TrackNodeBuilder WithRotation(Quaternion rotation) { Rotation = rotation; return this; }
  public TrackNodeBuilder WithRotation(float y) => WithRotation(Euler.x, y, Euler.z);
  public TrackNodeBuilder Level() => WithRotation(0, Euler.y, 0);
  public TrackNodeBuilder WithElevation(float elevation) => At(Position.x, elevation, Position.z);
  public TrackNodeBuilder WithPitch(float pitch) => WithRotation(pitch, Euler.y, Euler.z);
  public TrackNodeBuilder WithBank(float bank) => WithRotation(Euler.x, Euler.y, bank);
  public TrackNodeBuilder WithFlipSwitchStand(bool flip = true) { FlipSwitchStand = flip; return this; }
  public TrackNodeBuilder WithTurntable(Turntable turntable) { Turntable = turntable; return this; }
  public TrackSegmentBuilder ConnectTo(TrackNode otherNode) => Parent
      .CreateTrackSegment(Helper.IDs.Segment.Next(), Node, otherNode)
      .WithStyle(Parent.SegmentStyle)
      .WithGroupId(Parent.GroupId);
  public TrackNodeBuilder Extend(float length, float startAngle = 0, float endAngle = 0) => Extend(length, startAngle, endAngle, _ => { });
  public TrackNodeBuilder Extend(float length, float startAngle, float endAngle, Action<TrackSegmentBuilder> segmentAction) {
    var ret = Extend(length, startAngle, endAngle, out var builder);
    segmentAction(builder);
    return ret;
  }
  public TrackNodeBuilder Extend(float length, float startAngle, float endAngle, out TrackSegmentBuilder segmentBuilder)
  {
    var dir = Rotation * Quaternion.AngleAxis(startAngle, Vector3.up) * Vector3.forward;
    var pos = Position + (dir * length);
    var rot = Rotation * Quaternion.Euler(0, endAngle, 0);
    Logger.Information("Position: {Position}, Rotation: {Rotation}", Position, Rotation.eulerAngles);
    Logger.Information("Dir: {Dir}, Pos: {Pos}, Rot: {Rot}", dir, pos, rot.eulerAngles);
    var newNode = Parent
      .TrackNode(Helper.IDs.Node.Next())
      .At(pos)
      .WithRotation(rot);
    segmentBuilder = newNode.ConnectTo(Node);
    Logger.Information("Extended node {NodeId} by {Length}m to new node {NewNodeId} at {Position} with rotation {Rotation}", Id, length, newNode.Id, pos, rot.eulerAngles);
    return newNode;
  }

  // Create a curve with specific radius and angle
  public TrackNodeBuilder Curve(float radius, float angle)
  {
    // Calculate end position and rotation along the curve
    var startDirection = Node.transform.rotation * Vector3.forward;
    var endRotation = Node.transform.rotation * Quaternion.Euler(0, angle, 0);

    // Position the end node using circle geometry
    // Center is perpendicular to start direction
    var centerOffset = Quaternion.AngleAxis(90 * Mathf.Sign(angle), Vector3.up) * startDirection * radius;
    var centerPoint = Position + centerOffset;

    // End position is rotated around the center
    var endDirection = Quaternion.AngleAxis(angle, Vector3.up) * startDirection;
    var endOffset = Quaternion.AngleAxis(-90 * Mathf.Sign(angle), Vector3.up) * endDirection * radius;
    var endPosition = centerPoint + endOffset;

    // Create the end node
    var endNode = Parent
      .CreateTrackNode(Helper.IDs.Node.Next())
      .At(endPosition)
      .WithRotation(endRotation);

    // Create the segment connecting the nodes
    var segment = ConnectTo(endNode.Node);

    return endNode;
  }

  // Create a curve with specific radius and arc length
  public TrackNodeBuilder CurveByLength(float radius, float arcLength)
  {
    // Calculate angle from arc length: angle = arcLength / radius
    var angle = arcLength / radius * Mathf.Rad2Deg;
    return Curve(radius, angle);
  }

  // Create a curve with specific radius, arc length, and direction
  public TrackNodeBuilder CurveByLength(float radius, float arcLength, bool curveLeft)
  {
    // Calculate angle from arc length: angle = arcLength / radius
    var angle = Mathf.Abs(arcLength) / radius * Mathf.Rad2Deg;
    if (curveLeft) angle = -angle;
    return Curve(radius, angle);
  }
}
