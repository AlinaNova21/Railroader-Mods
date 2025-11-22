using System.Linq;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Definitions;

public partial class SerializedTurntable : IValidatable
{
  public string Identifier { get; set; } = "";

  public int Radius { get; set; } = 15;
  public int Subdivisions { get; set; } = 32;
  public SerializedVector3 Position { get; set; }
  public SerializedVector3 Rotation { get; set; }

  public int RoundhouseStalls { get; set; } = 0;
  public int RoundhouseTrackLength { get; set; } = 46;

  public string StallPrefab { get; set; } = "vanilla://roundhouseStall";
  public string StartPrefab { get; set; } = "vanilla://roundhouseStart";
  public string EndPrefab { get; set; } = "vanilla://roundhouseEnd";

  public SerializedTurntable()
  {
    Position = new SerializedVector3();
    Rotation = new SerializedVector3();
  }

  public void Validate()
  {
    var result = ValidateWithDetails();
    if (!result.IsValid)
    {
      var firstError = result.Errors.FirstOrDefault();
      throw new ValidationException(firstError?.Message ?? "Validation failed");
    }
  }

  public ValidationResult ValidateWithDetails()
  {
    return new ValidationResultCombiner()
      .Add(new ValidationBuilder<int>(nameof(Radius))
        .GreaterThanOrEqual(5)
        .Custom((radius, context) =>
        {
          var result = new ValidationResult { IsValid = true };
          if (radius > 50)
          {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
              Field = context.FieldName,
              Message = "Radius must be between 5 and 50",
              Code = "RADIUS_OUT_OF_RANGE",
              Value = radius
            });
          }
          return result;
        }), Radius)
      .Add(new ValidationBuilder<int>(nameof(Subdivisions))
        .GreaterThanOrEqual(1)
        .Custom((subdivisions, context) =>
        {
          var result = new ValidationResult { IsValid = true };
          if (subdivisions > 50)
          {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
              Field = context.FieldName,
              Message = "Subdivisions must be between 1 and 50",
              Code = "SUBDIVISIONS_OUT_OF_RANGE",
              Value = subdivisions
            });
          }
          return result;
        }), Subdivisions)
      .Add(new ValidationBuilder<int>(nameof(RoundhouseStalls))
        .GreaterThanOrEqual(0)
        .Custom((stalls, context) =>
        {
          var result = new ValidationResult { IsValid = true };
          if (stalls > Subdivisions)
          {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
              Field = context.FieldName,
              Message = $"Roundhouse stalls ({stalls}) cannot exceed subdivisions ({Subdivisions})",
              Code = "STALLS_EXCEED_SUBDIVISIONS",
              Value = stalls
            });
          }
          return result;
        }), RoundhouseStalls)
      .Add(new ValidationBuilder<string>(nameof(StartPrefab))
        .Required()
        .AsGameObjectUri(VanillaPrefabs.AvailableRoundhousePrefabs), StartPrefab)
      .Add(new ValidationBuilder<string>(nameof(EndPrefab))
        .Required()
        .AsGameObjectUri(VanillaPrefabs.AvailableRoundhousePrefabs), EndPrefab)
      .Add(new ValidationBuilder<string>(nameof(StallPrefab))
        .Required()
        .AsGameObjectUri(VanillaPrefabs.AvailableRoundhousePrefabs), StallPrefab)
      .Add(new ValidationBuilder<SerializedVector3>(nameof(Position))
        .Required(), Position)
      .Add(new ValidationBuilder<SerializedVector3>(nameof(Rotation))
        .Required(), Rotation)
      .Result;
  }
}
