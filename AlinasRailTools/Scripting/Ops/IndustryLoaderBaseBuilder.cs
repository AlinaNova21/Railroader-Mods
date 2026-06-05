using AlinasRailTools.Scripting;
using Model.Ops;
using Model.Ops.Definition;

namespace AlinasRailTools.Scripting.Ops;

public abstract class IndustryLoaderBaseBuilder<TBuilder> : BaseIndustryComponentBuilder<TBuilder>
  where TBuilder : IndustryLoaderBaseBuilder<TBuilder>
{
  public IndustryLoaderBase LoaderBase => (IndustryLoaderBase)Component;

  protected IndustryLoaderBaseBuilder(IndustryBuilder parent, IndustryLoaderBase component)
    : base(parent, component) { }

  // Properties for IndustryLoaderBase fields
  public Load Load { get => LoaderBase.load; set { LoaderBase.load = value; Invalidate(); } }
  public float ProductionRate { get => LoaderBase.productionRate; set { LoaderBase.productionRate = value; Invalidate(); } }
  public float MaxStorage { get => LoaderBase.maxStorage; set { LoaderBase.maxStorage = value; Invalidate(); } }
  public bool OrderEmpties { get => LoaderBase.orderEmpties; set { LoaderBase.orderEmpties = value; Invalidate(); } }

  // Fluent methods
  public TBuilder WithLoad(Load load) { Load = load; return (TBuilder)this; }
  public TBuilder WithProductionRate(float productionRate) { ProductionRate = productionRate; return (TBuilder)this; }
  public TBuilder WithMaxStorage(float maxStorage) { MaxStorage = maxStorage; return (TBuilder)this; }
  public TBuilder WithOrderEmpties(bool orderEmpties = true) { OrderEmpties = orderEmpties; return (TBuilder)this; }
}