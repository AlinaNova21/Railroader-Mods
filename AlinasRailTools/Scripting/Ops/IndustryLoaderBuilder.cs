using AlinasRailTools.Scripting;
using Model.Ops;

namespace AlinasRailTools.Scripting.Ops;

public class IndustryLoaderBuilder : IndustryLoaderBaseBuilder<IndustryLoaderBuilder>
{
  public IndustryLoader Loader => (IndustryLoader)Component;

  public IndustryLoaderBuilder(IndustryBuilder parent, IndustryLoader component)
    : base(parent, component) { }

  // Properties for IndustryLoader fields
  public float CarLoadRate { get => Loader.carLoadRate; set { Loader.carLoadRate = value; Invalidate(); } }
  public bool OrderAwayLoaded { get => Loader.orderAwayLoaded; set { Loader.orderAwayLoaded = value; Invalidate(); } }

  // Fluent methods
  public IndustryLoaderBuilder WithCarLoadRate(float carLoadRate) { CarLoadRate = carLoadRate; return this; }
  public IndustryLoaderBuilder WithOrderAwayLoaded(bool orderAwayLoaded = true) { OrderAwayLoaded = orderAwayLoaded; return this; }
}