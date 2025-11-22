using System;
using System.Collections.Generic;
using System.Linq;

namespace AlinasMapMod.Validation;

/// <summary>
/// Fluent builder for configuring validation rules on a field
/// </summary>
public class ValidationBuilder<T>
{
    private readonly List<ValidationRule<T>> _rules = new List<ValidationRule<T>>();
    private readonly string _fieldName;
        
    public ValidationBuilder(string fieldName)
    {
        _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
    }
        
    /// <summary>
    /// Adds a required validation rule
    /// </summary>
    public ValidationBuilder<T> Required(bool allowWhitespace = false)
    {
        _rules.Add(new RequiredRule<T>(allowWhitespace));
        return this;
    }
        
    /// <summary>
    /// Adds a custom validation rule using a delegate
    /// </summary>
    public ValidationBuilder<T> Custom(Func<T, ValidationContext, ValidationResult> validator)
    {
        _rules.Add(new CustomRule<T>(validator));
        return this;
    }
        
    /// <summary>
    /// Adds a validation rule to check if value is in the allowed list
    /// </summary>
    public ValidationBuilder<T> OneOf(params T[] allowedValues)
    {
        _rules.Add(new WhitelistRule<T>(allowedValues));
        return this;
    }
        
    /// <summary>
    /// Adds a validation rule to check if value is in the allowed list
    /// </summary>
    public ValidationBuilder<T> OneOf(IEnumerable<T> allowedValues)
    {
        _rules.Add(new WhitelistRule<T>(allowedValues));
        return this;
    }
        
    /// <summary>
    /// Validates the value against all configured rules
    /// </summary>
    public ValidationResult Validate(T value, ValidationContext context = null)
    {
        context = context ?? new ValidationContext();
        context.FieldName = _fieldName;
            
        var combinedResult = new ValidationResult { IsValid = true };
            
        foreach (var rule in _rules)
        {
            var result = rule.Validate(value, context);
            if (!result.IsValid)
            {
                combinedResult.IsValid = false;
                combinedResult.Errors.AddRange(result.Errors);
            }
            combinedResult.Warnings.AddRange(result.Warnings);
        }
            
        return combinedResult;
    }
        
    /// <summary>
    /// Internal method to add rules directly (used by extension methods)
    /// </summary>
    internal ValidationBuilder<T> AddRule(ValidationRule<T> rule)
    {
        _rules.Add(rule);
        return this;
    }
}
    
/// <summary>
/// Helper class for combining multiple validation results
/// </summary>
public class ValidationResultCombiner
{
    private readonly ValidationResult _combinedResult = new ValidationResult { IsValid = true };
        
    /// <summary>
    /// Adds a validation result to the combined result
    /// </summary>
    public ValidationResultCombiner Add(ValidationResult result)
    {
        if (!result.IsValid)
        {
            _combinedResult.IsValid = false;
            _combinedResult.Errors.AddRange(result.Errors);
        }
        _combinedResult.Warnings.AddRange(result.Warnings);
        return this;
    }
        
    /// <summary>
    /// Adds a validation builder result to the combined result
    /// </summary>
    public ValidationResultCombiner Add<T>(ValidationBuilder<T> builder, T value)
    {
        return Add(builder.Validate(value));
    }
        
    /// <summary>
    /// Gets the final combined validation result
    /// </summary>
    public ValidationResult Result => _combinedResult;
}
    
/// <summary>
/// Extension methods for common validation patterns
/// </summary>
public static class ValidationBuilderExtensions
{
    /// <summary>
    /// Validates that a string follows URI format
    /// </summary>
    public static ValidationBuilder<string> AsUri(this ValidationBuilder<string> builder)
    {
        return builder.AddRule(new UriFormatRule());
    }
        
    /// <summary>
    /// Validates vanilla prefabs against allowed prefab list
    /// </summary>
    public static ValidationBuilder<string> AsVanillaPrefab(this ValidationBuilder<string> builder, string[] allowedPrefabs)
    {
        return builder.AddRule(new VanillaPrefabRule(allowedPrefabs));
    }
        
    /// <summary>
    /// Validates that a value exists in a cache
    /// </summary>
    public static ValidationBuilder<string> ExistsInCache<TCache>(this ValidationBuilder<string> builder, 
        Func<string, bool> cacheContainsFunc, string cacheTypeName)
    {
        return builder.AddRule(new CacheValidationRule<TCache>(cacheContainsFunc, cacheTypeName));
    }
        
    /// <summary>
    /// Validates URI scheme is supported by Utils.GameObjectFromUri (path, scenery, vanilla, empty)
    /// </summary>
    public static ValidationBuilder<string> AsUriScheme(this ValidationBuilder<string> builder)
    {
        return builder.AddRule(new UriSchemeValidationRule());
    }
        
    /// <summary>
    /// Validates path:// URI format (path://scene/GameObject1/GameObject2/...)
    /// </summary>
    public static ValidationBuilder<string> AsPathUri(this ValidationBuilder<string> builder)
    {
        return builder.AddRule(new PathUriRule());
    }
        
    /// <summary>
    /// Validates scenery:// URI format (scenery://identifier)
    /// </summary>
    public static ValidationBuilder<string> AsSceneryUri(this ValidationBuilder<string> builder)
    {
        return builder.AddRule(new SceneryUriRule());
    }
        
    /// <summary>
    /// Validates empty:// URI format (minimal validation)
    /// </summary>
    public static ValidationBuilder<string> AsEmptyUri(this ValidationBuilder<string> builder)
    {
        return builder.AddRule(new EmptyUriRule());
    }
        
    /// <summary>
    /// Comprehensive GameObject URI validation for all schemes supported by Utils.GameObjectFromUri
    /// Validates path://, scenery://, vanilla://, and empty:// URIs with appropriate format checking
    /// </summary>
    public static ValidationBuilder<string> AsGameObjectUri(this ValidationBuilder<string> builder, string[] allowedVanillaPrefabs = null)
    {
        return builder.AddRule(new GameObjectUriRule(allowedVanillaPrefabs));
    }
        
    /// <summary>
    /// Validates that an enum value is defined
    /// </summary>
    public static ValidationBuilder<int> AsValidEnum<TEnum>(this ValidationBuilder<int> builder) where TEnum : System.Enum
    {
        return builder.AddRule(new EnumValidationRule<TEnum>());
    }
        
    /// <summary>
    /// Validates that a numeric value is greater than a minimum
    /// </summary>
    public static ValidationBuilder<T> GreaterThan<T>(this ValidationBuilder<T> builder, T minValue) where T : System.IComparable<T>
    {
        return builder.AddRule(new MinValueRule<T>(minValue, false));
    }
        
    /// <summary>
    /// Validates that a numeric value is greater than or equal to a minimum
    /// </summary>
    public static ValidationBuilder<T> GreaterThanOrEqual<T>(this ValidationBuilder<T> builder, T minValue) where T : System.IComparable<T>
    {
        return builder.AddRule(new MinValueRule<T>(minValue, true));
    }
        
    /// <summary>
    /// Validates that a cached object is of a specific type
    /// </summary>
    public static ValidationBuilder<string> OfCacheType<TExpected>(this ValidationBuilder<string> builder, 
        Func<string, object> getCachedObjectFunc, string cacheTypeName)
    {
        return builder.AddRule(new CacheTypeValidationRule<TExpected>(getCachedObjectFunc, cacheTypeName));
    }
        
    /// <summary>
    /// Validates that a string is required and not whitespace-only (convenience method)
    /// </summary>
    public static ValidationBuilder<string> RequiredNotWhitespace(this ValidationBuilder<string> builder)
    {
        return builder.Required(allowWhitespace: false);
    }
}