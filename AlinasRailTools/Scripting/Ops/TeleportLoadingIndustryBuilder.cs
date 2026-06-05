using System.Linq;
using AlinasRailTools.Scripting;
using AlinasRailTools.Scripting.Graph;
using Model.Ops;
using Track;

namespace AlinasRailTools.Scripting.Ops;

public class TeleportLoadingIndustryBuilder : IndustryLoaderBaseBuilder<TeleportLoadingIndustryBuilder>
{
  public TeleportLoadingIndustry TeleportLoader => (TeleportLoadingIndustry)Component;

  public TeleportLoadingIndustryBuilder(IndustryBuilder parent, TeleportLoadingIndustry component)
    : base(parent, component) { }

  // Properties for TeleportLoadingIndustry fields
  public float CarLoadPeriod { get => TeleportLoader.carLoadPeriod; set { TeleportLoader.carLoadPeriod = value; Invalidate(); } }
  public TrackSpan[] InputSpans { get => TeleportLoader.inputSpans; set { TeleportLoader.inputSpans = value; Invalidate(); } }
  public TrackSpan[] OutputSpans { get => TeleportLoader.outputSpans; set { TeleportLoader.outputSpans = value; Invalidate(); } }
  public float CarLengthFeet { get => TeleportLoader.carLengthFeet; set { TeleportLoader.carLengthFeet = value; Invalidate(); } }

  // Fluent methods
  public TeleportLoadingIndustryBuilder WithCarLoadPeriod(float carLoadPeriod) { CarLoadPeriod = carLoadPeriod; return this; }
  public TeleportLoadingIndustryBuilder WithInputSpans(params TrackSpan[] inputSpans) { InputSpans = inputSpans; return this; }
  public TeleportLoadingIndustryBuilder WithInputSpans(params TrackSpanBuilder[] inputSpanBuilders) => WithInputSpans(inputSpanBuilders.Select(b => b.Span).ToArray());
  public TeleportLoadingIndustryBuilder WithOutputSpans(params TrackSpan[] outputSpans) { OutputSpans = outputSpans; return this; }
  public TeleportLoadingIndustryBuilder WithOutputSpans(params TrackSpanBuilder[] outputSpanBuilders) => WithOutputSpans(outputSpanBuilders.Select(b => b.Span).ToArray());
  public TeleportLoadingIndustryBuilder WithCarLengthFeet(float carLengthFeet) { CarLengthFeet = carLengthFeet; return this; }
}