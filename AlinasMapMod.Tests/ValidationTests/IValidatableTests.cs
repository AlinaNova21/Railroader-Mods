using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Tests.ValidationTests;
    [TestClass]
    public class IValidatableTests
    {
        private class TestValidatableObject : IValidatable
        {
            public bool ShouldFailValidation { get; set; }
            public string ErrorMessage { get; set; } = "Test validation failed";
            public ValidationError[] Errors { get; set; } = new ValidationError[0];
            public ValidationWarning[] Warnings { get; set; } = new ValidationWarning[0];

            public void Validate()
            {
                if (ShouldFailValidation)
                {
                    throw new ValidationException(ErrorMessage);
                }
            }

            public ValidationResult ValidateWithDetails()
            {
                var result = new ValidationResult { IsValid = !ShouldFailValidation };
                
                if (ShouldFailValidation)
                {
                    result.Errors.AddRange(Errors);
                }
                
                result.Warnings.AddRange(Warnings);
                return result;
            }
        }

        [TestMethod]
        public void SimpleValidation_Success_DoesNotThrow()
        {
            // Arrange
            var validatable = new TestValidatableObject { ShouldFailValidation = false };

            // Act & Assert
            validatable.Validate(); // Should not throw
        }

        [TestMethod]
        public void SimpleValidation_Failure_ThrowsValidationException()
        {
            // Arrange
            var validatable = new TestValidatableObject 
            { 
                ShouldFailValidation = true,
                ErrorMessage = "Custom error message"
            };

            // Act & Assert
            var exception = Assert.ThrowsException<ValidationException>(() => validatable.Validate());
            Assert.AreEqual("Custom error message", exception.Message);
        }

        [TestMethod]
        public void RichValidation_Success_ReturnsValidResult()
        {
            // Arrange
            var validatable = new TestValidatableObject { ShouldFailValidation = false };

            // Act
            var result = validatable.ValidateWithDetails();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        [TestMethod]
        public void RichValidation_Failure_ReturnsInvalidResultWithErrors()
        {
            // Arrange
            var validatable = new TestValidatableObject 
            { 
                ShouldFailValidation = true,
                Errors = new[] 
                { 
                    new ValidationError 
                    { 
                        Field = "TestField", 
                        Message = "Test error", 
                        Code = "TEST_ERROR",
                        Value = "invalid_value"
                    }
                }
            };

            // Act
            var result = validatable.ValidateWithDetails();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("TestField", result.Errors[0].Field);
            Assert.AreEqual("Test error", result.Errors[0].Message);
            Assert.AreEqual("TEST_ERROR", result.Errors[0].Code);
            Assert.AreEqual("invalid_value", result.Errors[0].Value);
        }

        [TestMethod]
        public void RichValidation_WithWarnings_ReturnsValidResultWithWarnings()
        {
            // Arrange
            var validatable = new TestValidatableObject 
            { 
                ShouldFailValidation = false,
                Warnings = new[] 
                { 
                    new ValidationWarning 
                    { 
                        Field = "TestField", 
                        Message = "Test warning", 
                        Value = "warning_value"
                    }
                }
            };

            // Act
            var result = validatable.ValidateWithDetails();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.AreEqual(1, result.Warnings.Count);
            Assert.AreEqual("TestField", result.Warnings[0].Field);
            Assert.AreEqual("Test warning", result.Warnings[0].Message);
            Assert.AreEqual("warning_value", result.Warnings[0].Value);
        }

        [TestMethod]
        public void DualValidationModes_ConsistentBehavior()
        {
            // Test that both validation modes are consistent for the same object state
            
            // Arrange - Valid state
            var validObject = new TestValidatableObject { ShouldFailValidation = false };
            
            // Act & Assert - Valid state
            validObject.Validate(); // Should not throw
            var validResult = validObject.ValidateWithDetails();
            Assert.IsTrue(validResult.IsValid);
            
            // Arrange - Invalid state
            var invalidObject = new TestValidatableObject 
            { 
                ShouldFailValidation = true,
                ErrorMessage = "Consistency test error",
                Errors = new[] { new ValidationError { Message = "Consistency test error" } }
            };
            
            // Act & Assert - Invalid state
            Assert.ThrowsException<ValidationException>(() => invalidObject.Validate());
            var invalidResult = invalidObject.ValidateWithDetails();
            Assert.IsFalse(invalidResult.IsValid);
            Assert.AreEqual(1, invalidResult.Errors.Count);
        }
    }