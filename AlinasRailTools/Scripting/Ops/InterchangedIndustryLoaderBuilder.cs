using AlinasRailTools.Scripting;
using Model.Ops;
using Model.Ops.Definition;

namespace AlinasRailTools.Scripting.Ops;

public class InterchangedIndustryLoaderBuilder : BaseIndustryComponentBuilder<InterchangedIndustryLoaderBuilder>
{
  public InterchangedIndustryLoader InterchangedLoader => (InterchangedIndustryLoader)Component;

  public InterchangedIndustryLoaderBuilder(IndustryBuilder parent, InterchangedIndustryLoader component)
    : base(parent, component) { }

  // Properties for InterchangedIndustryLoader fields
  public Load Load { get => InterchangedLoader.load; set { InterchangedLoader.load = value; Invalidate(); } }

  // Fluent methods
  public InterchangedIndustryLoaderBuilder WithLoad(Load load) { Load = load; return this; }
}