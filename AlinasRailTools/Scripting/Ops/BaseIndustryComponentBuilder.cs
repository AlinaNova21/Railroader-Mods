using System.Linq;
using Model.Ops;
using AlinasRailTools.Scripting;
using AlinasRailTools.Scripting.Graph;
using Track;
using UnityEngine;

namespace AlinasRailTools.Scripting.Ops;

public abstract class BaseIndustryComponentBuilder<TBuilder> : BaseBuilder<IndustryBuilder, TBuilder>
  where TBuilder : BaseIndustryComponentBuilder<TBuilder>
{
  public IndustryComponent Component { get; }
  public Helper Helper => Parent.Helper;

  protected BaseIndustryComponentBuilder(IndustryBuilder parent, IndustryComponent component)
    : base(parent, component.subIdentifier) => Component = component;

  public override IndustryBuilder Remove() => Parent.RemoveComponent(Id);

  // Properties for IndustryComponent fields
  public TrackSpan[] TrackSpans { get => Component.trackSpans; set { Component.trackSpans = value; Invalidate(); } }
  public CarTypeFilter CarTypeFilter { get => Component.carTypeFilter; set { Component.carTypeFilter = value; Invalidate(); } }
  public bool SharedStorage { get => Component.sharedStorage; set { Component.sharedStorage = value; Invalidate(); } }
  public string Name { get => Component.name; set { Component.name = value; } }

  // Fluent methods
  public TBuilder WithTrackSpans(params TrackSpan[] trackSpans) { TrackSpans = trackSpans; return (TBuilder)this; }
  public TBuilder WithTrackSpans(params TrackSpanBuilder[] trackSpanBuilders) => WithTrackSpans(trackSpanBuilders.Select(b => b.Span).ToArray());
  public TBuilder WithCarTypeFilter(CarTypeFilter carTypeFilter) { CarTypeFilter = carTypeFilter; return (TBuilder)this; }
  public TBuilder WithCarTypeFilter(string filter) => WithCarTypeFilter(new CarTypeFilter(filter));
  public TBuilder WithSharedStorage(bool sharedStorage = true) { SharedStorage = sharedStorage; return (TBuilder)this; }
  public TBuilder WithName(string name) { Name = name; return (TBuilder)this; }
  public override TBuilder SetActive(bool active = true) { Component.gameObject.SetActive(active); return (TBuilder)this; }
}