using AlinasRailTools.Scripting;
using Model.Ops;
using Model.Ops.Definition;

namespace AlinasRailTools.Scripting.Ops;

public class LoadExporterBuilder : BaseIndustryComponentBuilder<LoadExporterBuilder>
{
  public LoadExporter Exporter => (LoadExporter)Component;

  public LoadExporterBuilder(IndustryBuilder parent, LoadExporter component)
    : base(parent, component) { }

  // Properties for LoadExporter fields
  public Load Load { get => Exporter.load; set { Exporter.load = value; Invalidate(); } }
  public float MinimumLoad { get => Exporter.minimumLoad; set { Exporter.minimumLoad = value; Invalidate(); } }

  // Fluent methods
  public LoadExporterBuilder WithLoad(Load load) { Load = load; return this; }
  public LoadExporterBuilder WithMinimumLoad(float minimumLoad) { MinimumLoad = minimumLoad; return this; }
}