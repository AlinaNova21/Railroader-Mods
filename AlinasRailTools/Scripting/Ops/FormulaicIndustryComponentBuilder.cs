using System.Collections.Generic;
using AlinasRailTools.Scripting;
using System.Linq;
using Model.Ops;
using Model.Ops.Definition;

namespace AlinasRailTools.Scripting.Ops;

public class FormulaicIndustryComponentBuilder : BaseIndustryComponentBuilder<FormulaicIndustryComponentBuilder>
{
  public FormulaicIndustryComponent FormulaicComponent => (FormulaicIndustryComponent)Component;

  public FormulaicIndustryComponentBuilder(IndustryBuilder parent, FormulaicIndustryComponent component)
    : base(parent, component) { }

  // Properties for FormulaicIndustryComponent fields
  public List<FormulaicIndustryComponent.Term> InputTerms { get => FormulaicComponent.inputTerms; set { FormulaicComponent.inputTerms = value; Invalidate(); } }
  public List<FormulaicIndustryComponent.Term> OutputTerms { get => FormulaicComponent.outputTerms; set { FormulaicComponent.outputTerms = value; Invalidate(); } }

  // Fluent methods for managing terms
  public FormulaicIndustryComponentBuilder WithInputTerms(params FormulaicIndustryComponent.Term[] terms)
  {
    InputTerms = terms.ToList();
    return this;
  }

  public FormulaicIndustryComponentBuilder WithOutputTerms(params FormulaicIndustryComponent.Term[] terms)
  {
    OutputTerms = terms.ToList();
    return this;
  }

  public FormulaicIndustryComponentBuilder AddInputTerm(Load load, float unitsPerDay = 1f)
  {
    InputTerms.Add(new FormulaicIndustryComponent.Term { load = load, unitsPerDay = unitsPerDay });
    Invalidate();
    return this;
  }

  public FormulaicIndustryComponentBuilder AddOutputTerm(Load load, float unitsPerDay = 1f)
  {
    OutputTerms.Add(new FormulaicIndustryComponent.Term { load = load, unitsPerDay = unitsPerDay });
    Invalidate();
    return this;
  }

  public FormulaicIndustryComponentBuilder ClearInputTerms()
  {
    InputTerms.Clear();
    Invalidate();
    return this;
  }

  public FormulaicIndustryComponentBuilder ClearOutputTerms()
  {
    OutputTerms.Clear();
    Invalidate();
    return this;
  }

  public FormulaicIndustryComponentBuilder RemoveInputTerm(Load load)
  {
    var term = InputTerms.FirstOrDefault(t => t.load == load);
    if (term != null)
    {
      InputTerms.Remove(term);
      Invalidate();
    }
    return this;
  }

  public FormulaicIndustryComponentBuilder RemoveOutputTerm(Load load)
  {
    var term = OutputTerms.FirstOrDefault(t => t.load == load);
    if (term != null)
    {
      OutputTerms.Remove(term);
      Invalidate();
    }
    return this;
  }
}