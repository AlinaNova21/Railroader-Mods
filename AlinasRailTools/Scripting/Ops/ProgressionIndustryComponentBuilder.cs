using AlinasRailTools.Scripting;
using Model.Ops;

namespace AlinasRailTools.Scripting.Ops;

public class ProgressionIndustryComponentBuilder : BaseIndustryComponentBuilder<ProgressionIndustryComponentBuilder>
{
  public ProgressionIndustryComponent ProgressionComponent => (ProgressionIndustryComponent)Component;

  public ProgressionIndustryComponentBuilder(IndustryBuilder parent, ProgressionIndustryComponent component)
    : base(parent, component) { }

  // No additional properties - ProgressionIndustryComponent only has base IndustryComponent fields
}