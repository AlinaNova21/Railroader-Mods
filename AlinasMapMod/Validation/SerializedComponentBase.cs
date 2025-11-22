using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using AlinasMapMod.Definitions;

namespace AlinasMapMod.Validation;

  /// <summary>
  /// Base class for serialized components that provides standardized validation functionality
  /// </summary>
  public abstract class SerializedComponentBase<T> : ISerializedPatchableComponent<T>, IValidatable 
      where T : Component
  {
      private readonly Dictionary<string, Func<ValidationResult>> _validators = new Dictionary<string, Func<ValidationResult>>();
      private bool _validationConfigured = false;
      
      /// <summary>
      /// Override this method to configure validation rules for the component
      /// </summary>
      protected virtual void ConfigureValidation()
      {
          // Override in derived classes to set up validation rules
      }
      
      /// <summary>
      /// Creates a validation builder for a property
      /// </summary>
      protected ValidationBuilder<TValue> RuleFor<TValue>(Expression<Func<TValue>> propertyExpression)
      {
          if (!(propertyExpression.Body is MemberExpression memberExpression))
              throw new ArgumentException("Expression must be a property access", nameof(propertyExpression));
              
          var propertyName = memberExpression.Member.Name;
          var propertyInfo = GetType().GetProperty(propertyName);
          
          if (propertyInfo == null)
              throw new ArgumentException($"Property {propertyName} not found", nameof(propertyExpression));
          
          var builder = new ValidationBuilder<TValue>(propertyName);
          
          // Store the validator function
          _validators[propertyName] = () =>
          {
              var propertyValue = (TValue)propertyInfo.GetValue(this);
              var context = new ValidationContext { Owner = this };
              return builder.Validate(propertyValue, context);
          };
          
          return builder;
      }
      
      /// <summary>
      /// Simple validation that throws ValidationException on failure (maintains backward compatibility)
      /// </summary>
      public virtual void Validate()
      {
          var result = ValidateWithDetails();
          result.ThrowIfInvalid();
      }
      
      /// <summary>
      /// Rich validation that returns detailed results including errors and warnings
      /// </summary>
      public virtual ValidationResult ValidateWithDetails()
      {
          if (!_validationConfigured)
          {
              ConfigureValidation();
              _validationConfigured = true;
          }
          
          var results = _validators.Values.Select(validator => validator()).ToArray();
          return ValidationResult.Combine(results);
      }
      
      /// <summary>
      /// Creates the component instance (must be implemented by derived classes)
      /// </summary>
      public abstract T Create(string id);
      
      /// <summary>
      /// Writes data to the component (must be implemented by derived classes)
      /// </summary>
      public abstract void Write(T comp);
      
      /// <summary>
      /// Reads data from the component (must be implemented by derived classes)
      /// </summary>
      public abstract void Read(T comp);
  }
