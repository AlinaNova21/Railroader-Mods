using AlinasRailTools.Scripting;
using Model.Ops;
using Model.Ops.Definition;

namespace AlinasRailTools.Scripting.Ops;

public class IndustryUnloaderBuilder : BaseIndustryComponentBuilder<IndustryUnloaderBuilder>
{
  public IndustryUnloader Unloader => (IndustryUnloader)Component;

  public IndustryUnloaderBuilder(IndustryBuilder parent, IndustryUnloader component)
    : base(parent, component) { }

  // Properties for IndustryUnloader fields
  public Load Load { get => Unloader.load; set { Unloader.load = value; Invalidate(); } }
  public float CarUnloadRate { get => Unloader.carUnloadRate; set { Unloader.carUnloadRate = value; Invalidate(); } }
  public float StorageConsumptionRate { get => Unloader.storageConsumptionRate; set { Unloader.storageConsumptionRate = value; Invalidate(); } }
  public float MaxStorage { get => Unloader.maxStorage; set { Unloader.maxStorage = value; Invalidate(); } }
  public bool OrderAwayEmpties { get => Unloader.orderAwayEmpties; set { Unloader.orderAwayEmpties = value; Invalidate(); } }
  public bool OrderLoads { get => Unloader.orderLoads; set { Unloader.orderLoads = value; Invalidate(); } }

  // Fluent methods
  public IndustryUnloaderBuilder WithLoad(Load load) { Load = load; return this; }
  public IndustryUnloaderBuilder WithCarUnloadRate(float carUnloadRate) { CarUnloadRate = carUnloadRate; return this; }
  public IndustryUnloaderBuilder WithStorageConsumptionRate(float storageConsumptionRate) { StorageConsumptionRate = storageConsumptionRate; return this; }
  public IndustryUnloaderBuilder WithMaxStorage(float maxStorage) { MaxStorage = maxStorage; return this; }
  public IndustryUnloaderBuilder WithOrderAwayEmpties(bool orderAwayEmpties = true) { OrderAwayEmpties = orderAwayEmpties; return this; }
  public IndustryUnloaderBuilder WithOrderLoads(bool orderLoads = true) { OrderLoads = orderLoads; return this; }
}