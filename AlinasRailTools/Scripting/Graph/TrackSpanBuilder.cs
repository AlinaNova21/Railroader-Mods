using Track;
using AlinasRailTools.Scripting;

namespace AlinasRailTools.Scripting.Graph;

public class TrackSpanBuilder : BaseBuilder<GraphBuilder, TrackSpanBuilder>
{
  public TrackSpan Span { get; }
  public Helper Helper => Parent.Helper;
  public TrackSpanBuilder(GraphBuilder graphBuilder, TrackSpan span): base(graphBuilder, span.id) => Span = span;
  public override GraphBuilder Remove() => Parent.RemoveTrackSpan(Id);
  public Location? Upper { get => Span.upper; set { Span.upper = value; Invalidate(); } }
  public Location? Lower { get => Span.lower; set { Span.lower = value; Invalidate(); } }
  public float Length => Span.Length;
  public TrackSpanBuilder WithUpper(Location upper) { Upper = upper; return this; }
  public TrackSpanBuilder WithLower(Location lower) { Lower = lower; return this; }
  public TrackSpan TrackSpan => Span;
}