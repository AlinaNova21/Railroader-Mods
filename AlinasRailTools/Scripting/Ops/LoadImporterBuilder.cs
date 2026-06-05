using AlinasRailTools.Scripting;
using Model.Ops;
using Model.Ops.Definition;

namespace AlinasRailTools.Scripting.Ops;

public class LoadImporterBuilder : BaseIndustryComponentBuilder<LoadImporterBuilder>
{
  public LoadImporter Importer => (LoadImporter)Component;

  public LoadImporterBuilder(IndustryBuilder parent, LoadImporter component)
    : base(parent, component) { }

  // Properties for LoadImporter fields
  public Load Load { get => Importer.load; set { Importer.load = value; Invalidate(); } }
  public int DesiredCarCount { get => Importer.desiredCarCount; set { Importer.desiredCarCount = value; Invalidate(); } }
  public int MaxOrderCount { get => Importer.maxOrderCount; set { Importer.maxOrderCount = value; Invalidate(); } }
  public IndustryComponent ForwardToOnArrival { get => Importer.forwardToOnArrival; set { Importer.forwardToOnArrival = value; Invalidate(); } }

  // Fluent methods
  public LoadImporterBuilder WithLoad(Load load) { Load = load; return this; }
  public LoadImporterBuilder WithDesiredCarCount(int desiredCarCount) { DesiredCarCount = desiredCarCount; return this; }
  public LoadImporterBuilder WithMaxOrderCount(int maxOrderCount) { MaxOrderCount = maxOrderCount; return this; }
  public LoadImporterBuilder WithForwardToOnArrival(IndustryComponent forwardTo) { ForwardToOnArrival = forwardTo; return this; }
}