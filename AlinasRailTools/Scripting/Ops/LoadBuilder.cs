using Model.Definition.Data;
using AlinasRailTools.Scripting;
using Model.Ops.Definition;

namespace AlinasRailTools.Scripting.Ops;

public class LoadBuilder : BaseBuilder<OpsBuilder, LoadBuilder>
{
  public Load Load { get; }
  public Helper Helper => Parent.Helper;

  public LoadBuilder(OpsBuilder opsBuilder, Load load) : base(opsBuilder, load.id) => Load = load;

  public override OpsBuilder Remove() => Parent.RemoveLoad(Id);

  public override LoadBuilder Invalidate() => this; // Loads don't need graph invalidation

  // Properties for Load fields
  public string Description { get => Load.description; set => Load.description = value; }
  public LoadUnits Units { get => Load.units; set => Load.units = value; }
  public float Density { get => Load.density; set => Load.density = value; }
  public float UnitWeightInPounds { get => Load.unitWeightInPounds; set => Load.unitWeightInPounds = value; }
  public bool Importable { get => Load.importable; set => Load.importable = value; }
  public float PayPerQuantity { get => Load.payPerQuantity; set => Load.payPerQuantity = value; }
  public float CostPerUnit { get => Load.costPerUnit; set => Load.costPerUnit = value; }

  // Fluent methods
  public LoadBuilder WithDescription(string description) { Description = description; return this; }
  public LoadBuilder WithUnits(LoadUnits units) { Units = units; return this; }
  public LoadBuilder WithDensity(float density) { Density = density; return this; }
  public LoadBuilder WithUnitWeightInPounds(float unitWeightInPounds) { UnitWeightInPounds = unitWeightInPounds; return this; }
  public LoadBuilder WithImportable(bool importable = true) { Importable = importable; return this; }
  public LoadBuilder WithPayPerQuantity(float payPerQuantity) { PayPerQuantity = payPerQuantity; return this; }
  public LoadBuilder WithCostPerUnit(float costPerUnit) { CostPerUnit = costPerUnit; return this; }
}