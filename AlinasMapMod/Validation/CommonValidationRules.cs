using System;
using System.Collections.Generic;
using System.Linq;

namespace AlinasMapMod.Validation;

  /// <summary>
  /// Validates that a value is not null or empty (and not whitespace-only for strings)
  /// </summary>
  public class RequiredRule<T> : ValidationRule<T>
  {
      private readonly bool _allowWhitespace;
      
      public RequiredRule(bool allowWhitespace = false)
      {
          _allowWhitespace = allowWhitespace;
      }
      
      public override ValidationResult Validate(T value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (value == null)
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"{context.FieldName} is required",
                  Code = "REQUIRED",
                  Value = value
              });
          }
          else if (value is string str)
          {
              if (string.IsNullOrEmpty(str))
              {
                  result.IsValid = false;
                  result.Errors.Add(new ValidationError
                  {
                      Field = context.FieldName,
                      Message = $"{context.FieldName} is required",
                      Code = "REQUIRED",
                      Value = value
                  });
              }
              else if (!_allowWhitespace && string.IsNullOrWhiteSpace(str))
              {
                  result.IsValid = false;
                  result.Errors.Add(new ValidationError
                  {
                      Field = context.FieldName,
                      Message = $"{context.FieldName} cannot be empty or whitespace only",
                      Code = "REQUIRED_NOT_WHITESPACE",
                      Value = value
                  });
              }
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates that a string follows URI format (contains "://")
  /// </summary>
  public class UriFormatRule : ValidationRule<string>
  {
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (!string.IsNullOrEmpty(value) && !value.Contains("://"))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Invalid URI format: {value}, must contain '://'",
                  Code = "INVALID_URI_FORMAT",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates that a value is in the allowed whitelist
  /// </summary>
  public class WhitelistRule<T> : ValidationRule<T>
  {
      private readonly IEnumerable<T> _allowedValues;
      
      public WhitelistRule(IEnumerable<T> allowedValues)
      {
          _allowedValues = allowedValues ?? throw new ArgumentNullException(nameof(allowedValues));
      }
      
      public override ValidationResult Validate(T value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (!_allowedValues.Contains(value))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Invalid value: {value}. Must be one of allowed values: {string.Join(", ", _allowedValues)}",
                  Code = "INVALID_VALUE",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates vanilla prefab URIs against allowed prefab lists
  /// </summary>
  public class VanillaPrefabRule : ValidationRule<string>
  {
      private readonly string[] _allowedPrefabs;
      
      public VanillaPrefabRule(string[] allowedPrefabs)
      {
          _allowedPrefabs = allowedPrefabs ?? throw new ArgumentNullException(nameof(allowedPrefabs));
      }
      
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (string.IsNullOrEmpty(value))
              return result;
          
          // First validate URI format
          var uriResult = new UriFormatRule().Validate(value, context);
          if (!uriResult.IsValid)
              return uriResult;
          
          // Only allow vanilla:// URIs
          if (!value.StartsWith("vanilla://"))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Only vanilla:// URIs are allowed, got: {value}",
                  Code = "NON_VANILLA_URI_NOT_ALLOWED",
                  Value = value
              });
              return result;
          }
          
          // Validate vanilla prefab name (extract first part before any path)
          var prefabPart = value.Substring("vanilla://".Length);
          var prefabName = prefabPart.Contains("/") ? prefabPart.Split('/')[0] : prefabPart;
          
          if (string.IsNullOrEmpty(prefabName))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = "Vanilla URI must specify a prefab name",
                  Code = "MISSING_VANILLA_PREFAB_NAME",
                  Value = value
              });
          }
          else if (!_allowedPrefabs.Contains(prefabName))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Invalid vanilla prefab: {prefabName}, must be one of: {string.Join(", ", _allowedPrefabs)}",
                  Code = "INVALID_VANILLA_PREFAB",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates that a cache contains the specified key
  /// </summary>
  public class CacheValidationRule<T> : ValidationRule<string>
  {
      private readonly Func<string, bool> _cacheContainsFunc;
      private readonly string _cacheTypeName;
      
      public CacheValidationRule(Func<string, bool> cacheContainsFunc, string cacheTypeName)
      {
          _cacheContainsFunc = cacheContainsFunc ?? throw new ArgumentNullException(nameof(cacheContainsFunc));
          _cacheTypeName = cacheTypeName ?? throw new ArgumentNullException(nameof(cacheTypeName));
      }
      
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (string.IsNullOrEmpty(value))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"{_cacheTypeName} key cannot be null or empty",
                  Code = "CACHE_KEY_NULL_OR_EMPTY",
                  Value = value
              });
              return result;
          }
          
          try
          {
              if (!_cacheContainsFunc(value))
              {
                  result.IsValid = false;
                  result.Errors.Add(new ValidationError
                  {
                      Field = context.FieldName,
                      Message = $"{_cacheTypeName} '{value}' not found",
                      Code = "CACHE_NOT_FOUND",
                      Value = value
                  });
              }
          }
          catch (Exception ex)
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Error accessing {_cacheTypeName}: {ex.Message}",
                  Code = "CACHE_ACCESS_ERROR",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Comprehensive URI scheme validation supporting all schemes used by Utils.GameObjectFromUri
  /// Validates: path://, scenery://, vanilla://, empty://
  /// </summary>
  public class UriSchemeValidationRule : ValidationRule<string>
  {
      private static readonly string[] SupportedSchemes = { "path", "scenery", "vanilla", "empty" };
      
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (string.IsNullOrEmpty(value))
              return result;
          
          // First validate basic URI format
          var uriResult = new UriFormatRule().Validate(value, context);
          if (!uriResult.IsValid)
              return uriResult;
          
          // Extract and validate scheme (case-insensitive)
          var schemeIndex = value.IndexOf("://", StringComparison.Ordinal);
          var scheme = value.Substring(0, schemeIndex).ToLowerInvariant();
          
          if (!SupportedSchemes.Contains(scheme))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Unsupported URI scheme: {scheme}. Must be one of: {string.Join(", ", SupportedSchemes)}",
                  Code = "INVALID_URI_SCHEME",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates path:// URIs to ensure proper scene path format
  /// Format: path://scene/GameObject1/GameObject2/...
  /// </summary>
  public class PathUriRule : ValidationRule<string>
  {
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (string.IsNullOrEmpty(value))
              return result;
          
          // Only allow path:// URIs
          if (!value.StartsWith("path://"))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Only path:// URIs are allowed, got: {value}",
                  Code = "NON_PATH_URI_NOT_ALLOWED",
                  Value = value
              });
              return result;
          }
          
          // Validate scene path format
          if (!value.StartsWith("path://scene/"))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Path URI must start with 'path://scene/': {value}",
                  Code = "INVALID_PATH_URI_FORMAT",
                  Value = value
              });
              return result;
          }
          
          // Validate path components
          var pathPortion = value.Substring("path://scene/".Length);
          if (string.IsNullOrEmpty(pathPortion))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Path URI must specify GameObject path after 'path://scene/': {value}",
                  Code = "MISSING_PATH_COMPONENTS",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates scenery:// URIs for scenery asset references
  /// Format: scenery://identifier
  /// </summary>
  public class SceneryUriRule : ValidationRule<string>
  {
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (string.IsNullOrEmpty(value))
              return result;
          
          // Only allow scenery:// URIs
          if (!value.StartsWith("scenery://"))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Only scenery:// URIs are allowed, got: {value}",
                  Code = "NON_SCENERY_URI_NOT_ALLOWED",
                  Value = value
              });
              return result;
          }
          
          // Validate identifier exists and doesn't contain paths
          var identifier = value.Substring("scenery://".Length);
          if (string.IsNullOrEmpty(identifier))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Scenery URI must specify identifier after 'scenery://': {value}",
                  Code = "MISSING_SCENERY_IDENTIFIER",
                  Value = value
              });
          }
          else if (identifier.Contains("/"))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Scenery URI identifier cannot contain paths: {value}",
                  Code = "SCENERY_IDENTIFIER_CONTAINS_PATH",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates that a URI field contains only empty:// URIs or is empty
  /// Format: empty:// (host/path ignored) - creates blank GameObjects
  /// </summary>
  public class EmptyUriRule : ValidationRule<string>
  {
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (string.IsNullOrEmpty(value))
              return result;
          
          // Only allow empty:// URIs or whitespace is invalid too
          if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrEmpty(value))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = "Whitespace-only values are not allowed",
                  Code = "WHITESPACE_NOT_ALLOWED",
                  Value = value
              });
          }
          else if (!value.StartsWith("empty://"))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Only empty:// URIs are allowed, got: {value}",
                  Code = "NON_EMPTY_URI_NOT_ALLOWED",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Comprehensive GameObject URI validation that combines all scheme-specific validations
  /// Validates all URI forms supported by Utils.GameObjectFromUri
  /// </summary>
  public class GameObjectUriRule : ValidationRule<string>
  {
      private readonly string[] _allowedVanillaPrefabs;
      
      public GameObjectUriRule(string[] allowedVanillaPrefabs = null)
      {
          _allowedVanillaPrefabs = allowedVanillaPrefabs;
      }
      
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (string.IsNullOrEmpty(value))
              return result;
          
          // Apply scheme validation first
          var schemeResult = new UriSchemeValidationRule().Validate(value, context);
          if (!schemeResult.IsValid)
              return schemeResult;
          
          // Apply scheme-specific validation
          if (value.StartsWith("path://"))
          {
              var pathResult = new PathUriRule().Validate(value, context);
              if (!pathResult.IsValid)
                  return pathResult;
          }
          else if (value.StartsWith("scenery://"))
          {
              var sceneryResult = new SceneryUriRule().Validate(value, context);
              if (!sceneryResult.IsValid)
                  return sceneryResult;
          }
          else if (value.StartsWith("vanilla://"))
          {
              if (_allowedVanillaPrefabs != null)
              {
                  var vanillaResult = new VanillaPrefabRule(_allowedVanillaPrefabs).Validate(value, context);
                  if (!vanillaResult.IsValid)
                      return vanillaResult;
              }
          }
          else if (value.StartsWith("empty://"))
          {
              var emptyResult = new EmptyUriRule().Validate(value, context);
              if (!emptyResult.IsValid)
                  return emptyResult;
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates that an enum value is defined
  /// </summary>
  public class EnumValidationRule<TEnum> : ValidationRule<int> where TEnum : System.Enum
  {
      public override ValidationResult Validate(int value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (!System.Enum.IsDefined(typeof(TEnum), value))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Invalid {typeof(TEnum).Name}: {value}",
                  Code = "INVALID_ENUM_VALUE",
                  Value = value
              });
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates that a numeric value is greater than a minimum
  /// </summary>
  public class MinValueRule<T> : ValidationRule<T> where T : System.IComparable<T>
  {
      private readonly T _minValue;
      private readonly bool _inclusive;
      
      public MinValueRule(T minValue, bool inclusive = true)
      {
          _minValue = minValue;
          _inclusive = inclusive;
      }
      
      public override ValidationResult Validate(T value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (value != null)
          {
              var comparison = value.CompareTo(_minValue);
              var isValid = _inclusive ? comparison >= 0 : comparison > 0;
              
              if (!isValid)
              {
                  result.IsValid = false;
                  result.Errors.Add(new ValidationError
                  {
                      Field = context.FieldName,
                      Message = $"Value must be {(_inclusive ? "greater than or equal to" : "greater than")} minimum value {_minValue}",
                      Code = "MIN_VALUE_VIOLATION",
                      Value = value
                  });
              }
          }
          
          return result;
      }
  }
  
  /// <summary>
  /// Validates that a cached object is of a specific type
  /// </summary>
  public class CacheTypeValidationRule<TExpected> : ValidationRule<string>
  {
      private readonly Func<string, object> _getCachedObjectFunc;
      private readonly string _cacheTypeName;
      
      public CacheTypeValidationRule(Func<string, object> getCachedObjectFunc, string cacheTypeName)
      {
          _getCachedObjectFunc = getCachedObjectFunc ?? throw new ArgumentNullException(nameof(getCachedObjectFunc));
          _cacheTypeName = cacheTypeName ?? throw new ArgumentNullException(nameof(cacheTypeName));
      }
      
      public override ValidationResult Validate(string value, ValidationContext context)
      {
          var result = new ValidationResult { IsValid = true };
          
          if (string.IsNullOrEmpty(value))
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"{_cacheTypeName} key cannot be null or empty",
                  Code = "CACHE_KEY_NULL_OR_EMPTY",
                  Value = value
              });
              return result;
          }
          
          try
          {
              var cachedObject = _getCachedObjectFunc(value);
              if (cachedObject == null)
              {
                  result.IsValid = false;
                  result.Errors.Add(new ValidationError
                  {
                      Field = context.FieldName,
                      Message = $"{_cacheTypeName} '{value}' not found or is null",
                      Code = "CACHE_OBJECT_NULL",
                      Value = value
                  });
              }
              else if (!(cachedObject is TExpected))
              {
                  result.IsValid = false;
                  result.Errors.Add(new ValidationError
                  {
                      Field = context.FieldName,
                      Message = $"{_cacheTypeName} '{value}' is not of type {typeof(TExpected).Name}",
                      Code = "INVALID_CACHE_OBJECT_TYPE",
                      Value = value
                  });
              }
          }
          catch (Exception ex)
          {
              result.IsValid = false;
              result.Errors.Add(new ValidationError
              {
                  Field = context.FieldName,
                  Message = $"Error accessing {_cacheTypeName} '{value}': {ex.Message}",
                  Code = "CACHE_ACCESS_ERROR",
                  Value = value
              });
          }
          
          return result;
      }
  }
