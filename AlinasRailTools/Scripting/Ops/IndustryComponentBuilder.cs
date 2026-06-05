using Model.Ops;
using AlinasRailTools.Scripting;

namespace AlinasRailTools.Scripting.Ops;

public class IndustryComponentBuilder : BaseIndustryComponentBuilder<IndustryComponentBuilder>
{
  public IndustryComponentBuilder(IndustryBuilder industryBuilder, IndustryComponent component)
    : base(industryBuilder, component) { }
}