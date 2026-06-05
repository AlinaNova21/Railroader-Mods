using AlinasRailTools.Scripting;
using Track;
using UnityEngine;

namespace AlinasRailTools.Scripting.Graph;

public class TrackSegmentBuilder: BaseBuilder<GraphBuilder, TrackSegmentBuilder>
{
  public TrackSegment Segment { get; }
  public Helper Helper => Parent.Helper;

  public TrackSegmentBuilder(GraphBuilder graphBuilder, TrackSegment segment) : base(graphBuilder, segment.id) => this.Segment = segment;
  public override GraphBuilder Remove() => Parent.RemoveTrackSegment(Id);

  public override TrackSegmentBuilder Invalidate() {
    // Use TrackNodeBuilder's deferred invalidation system
    var nodeBuilderA = new TrackNodeBuilder(Parent, Segment.a);
    var nodeBuilderB = new TrackNodeBuilder(Parent, Segment.b);
    nodeBuilderA.Invalidate();
    nodeBuilderB.Invalidate();
    return this;
  }
  public TrackNode StartNode => Segment.a;
  public TrackNode EndNode => Segment.b;
  public TrackSegment.Style Style { get => Segment.style; set { Segment.style = value; Invalidate(); } }
  public string GroupId { get => Segment.groupId; set { Segment.groupId = value; Invalidate(); } }
  public int Priority { get => Segment.priority; set { Segment.priority = value; Invalidate(); } }
  public int SpeedLimit { get => Segment.speedLimit; set { Segment.speedLimit = value; Invalidate(); } }
  public TrackClass TrackClass { get => Segment.trackClass; set { Segment.trackClass = value; Invalidate(); } }

  public float Length => Segment.GetLength();
  public Turntable Turntable { get => Segment.turntable; set { Segment.turntable = value; Invalidate(); } }

  public TrackSegmentBuilder WithStyle(TrackSegment.Style style) { Style = style; return this; }
  public TrackSegmentBuilder WithGroupId(string group) { GroupId = group; return this; }
  public TrackSegmentBuilder WithStartNode(TrackNode startNode) { Segment.a = startNode; return this; }
  public TrackSegmentBuilder WithStartNode(TrackNodeBuilder startNodeBuilder) => WithStartNode(startNodeBuilder.Node);
  public TrackSegmentBuilder WithEndNode(TrackNode endNode) { Segment.b = endNode; return this; }
  public TrackSegmentBuilder WithEndNode(TrackNodeBuilder endNodeBuilder) => WithEndNode(endNodeBuilder.Node);
  public TrackSegmentBuilder WithPriority(int priority) { Priority = priority; return this; }
  public TrackSegmentBuilder WithSpeedLimit(int speedLimit) { SpeedLimit = speedLimit; return this; }
  public TrackSegmentBuilder WithTrackClass(TrackClass trackClass) { TrackClass = trackClass; return this; }
  public TrackSegmentBuilder Reverse() { (Segment.a, Segment.b) = (Segment.b, Segment.a); return this; }
  public TrackSegmentBuilder WithTurntable(Turntable turntable) { Turntable = turntable; return this; }
  
  // Splits the segment at the given location, creating a new node and segment. The current segment will be modified to end at the new node.
  public TrackNodeBuilder SplitAtLocation(float distance, TrackSegment.End end = TrackSegment.End.A) => SplitAtLocation(new Track.Location(Segment, distance, end));
  // Splits the segment at the given location, creating a new node and segment. The current segment will be modified to end at the new node.
  public TrackNodeBuilder SplitAtLocation(Track.Location location)
  {
    var newNode = Parent
      .CreateTrackNode(Helper.IDs.Node.Next())
      .At(location.GetPosition())
      .WithRotation(location.GetRotation());
    Parent
      .CreateTrackSegment(Helper.IDs.Segment.Next(), newNode.Node, Segment.b)
      .WithStyle(Segment.style)
      .WithGroupId(Segment.groupId);
    Segment.b = newNode.Node;
    return newNode;
  }
  public Track.Location? LocationFromPoint(Vector3 point, float radius) => Segment.LocationFromPoint(point, radius);
  public Track.Location LocationFromDistance(float distance, TrackSegment.End end = TrackSegment.End.A) => new(Segment, distance, end);
}
