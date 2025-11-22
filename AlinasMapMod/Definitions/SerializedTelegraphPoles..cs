using System.Linq;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Definitions;

public class SerializedTelegraphPoles : IValidatable
{
    public int[] PolesToRaise { get; set; }
    public int[] PolesToMove { get; set; }
    public float[,] PoleMovement { get; set; }

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
        var combiner = new ValidationResultCombiner();

        // Validate PolesToRaise array
        if (PolesToRaise != null)
        {
            for (int i = 0; i < PolesToRaise.Length; i++)
            {
                combiner.Add(new ValidationBuilder<int>($"{nameof(PolesToRaise)}[{i}]")
                    .GreaterThanOrEqual(0), PolesToRaise[i]);
            }
        }

        // Validate PolesToMove array
        if (PolesToMove != null)
        {
            for (int i = 0; i < PolesToMove.Length; i++)
            {
                combiner.Add(new ValidationBuilder<int>($"{nameof(PolesToMove)}[{i}]")
                    .GreaterThanOrEqual(0), PolesToMove[i]);
            }
        }

        // Validate PoleMovement dimensions consistency
        if (PolesToMove != null && PoleMovement != null)
        {
            combiner.Add(new ValidationBuilder<float[,]>(nameof(PoleMovement))
                .Custom((value, context) =>
                {
                    var result = new ValidationResult { IsValid = true };
                    if (value != null && PolesToMove != null)
                    {
                        var expectedRows = PolesToMove.Length;
                        var actualRows = value.GetLength(0);
                        var actualCols = value.GetLength(1);
              
                        if (actualRows != expectedRows)
                        {
                            result.IsValid = false;
                            result.Errors.Add(new ValidationError
                            {
                                Field = context.FieldName,
                                Message = $"PoleMovement array must have {expectedRows} rows to match PolesToMove length, but has {actualRows}",
                                Code = "POLE_MOVEMENT_DIMENSION_MISMATCH",
                                Value = value
                            });
                        }
              
                        if (actualCols != 3) // Expecting x, y, z coordinates
                        {
                            result.IsValid = false;
                            result.Errors.Add(new ValidationError
                            {
                                Field = context.FieldName,
                                Message = $"PoleMovement array must have 3 columns (x, y, z), but has {actualCols}",
                                Code = "POLE_MOVEMENT_INVALID_COLUMNS",
                                Value = value
                            });
                        }
                    }
                    return result;
                }), PoleMovement);
        }

        return combiner.Result;
    }
}