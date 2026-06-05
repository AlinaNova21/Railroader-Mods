using AlinasRailTools.Scripting;
using Model.Ops;

namespace AlinasRailTools.Scripting.Ops;

public class InterchangeBuilder : BaseIndustryComponentBuilder<InterchangeBuilder>
{
  public Interchange Interchange => (Interchange)Component;

  public InterchangeBuilder(IndustryBuilder parent, Interchange component)
    : base(parent, component) { }

  // No additional properties - Interchange only has base IndustryComponent fields
}