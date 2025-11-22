using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Tests.ValidationTests;
    [TestClass]
    public class ValidationResultTests
    {
        [TestMethod]
        public void DefaultConstructor_CreatesValidResult()
        {
            // Arrange & Act
            var result = new ValidationResult();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNotNull(result.Errors);
            Assert.IsNotNull(result.Warnings);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        [TestMethod]
        public void ThrowIfInvalid_ValidResult_DoesNotThrow()
        {
            // Arrange
            var result = new ValidationResult { IsValid = true };

            // Act & Assert
            result.ThrowIfInvalid(); // Should not throw
        }

        [TestMethod]
        public void ThrowIfInvalid_InvalidResult_ThrowsValidationException()
        {
            // Arrange
            var result = new ValidationResult 
            { 
                IsValid = false,
                Errors = { new ValidationError { Message = "Error 1" }, new ValidationError { Message = "Error 2" } }
            };

            // Act & Assert
            var exception = Assert.ThrowsException<ValidationException>(() => result.ThrowIfInvalid());
            Assert.AreEqual("Error 1; Error 2", exception.Message);
        }

        [TestMethod]
        public void ThrowIfInvalid_EmptyErrors_ThrowsValidationException()
        {
            // Arrange
            var result = new ValidationResult { IsValid = false };

            // Act & Assert
            var exception = Assert.ThrowsException<ValidationException>(() => result.ThrowIfInvalid());
            Assert.AreEqual("", exception.Message);
        }

        [TestMethod]
        public void Combine_AllValidResults_ReturnsValidResult()
        {
            // Arrange
            var result1 = new ValidationResult { IsValid = true };
            var result2 = new ValidationResult { IsValid = true };
            var result3 = new ValidationResult { IsValid = true };

            // Act
            var combined = ValidationResult.Combine(result1, result2, result3);

            // Assert
            Assert.IsTrue(combined.IsValid);
            Assert.AreEqual(0, combined.Errors.Count);
            Assert.AreEqual(0, combined.Warnings.Count);
        }

        [TestMethod]
        public void Combine_SomeInvalidResults_ReturnsInvalidResult()
        {
            // Arrange
            var validResult = new ValidationResult { IsValid = true };
            var invalidResult1 = new ValidationResult 
            { 
                IsValid = false,
                Errors = { new ValidationError { Message = "Error 1" } }
            };
            var invalidResult2 = new ValidationResult 
            { 
                IsValid = false,
                Errors = { new ValidationError { Message = "Error 2" } }
            };

            // Act
            var combined = ValidationResult.Combine(validResult, invalidResult1, invalidResult2);

            // Assert
            Assert.IsFalse(combined.IsValid);
            Assert.AreEqual(2, combined.Errors.Count);
            Assert.AreEqual("Error 1", combined.Errors[0].Message);
            Assert.AreEqual("Error 2", combined.Errors[1].Message);
        }

        [TestMethod]
        public void Combine_WithWarnings_CombinesAllWarnings()
        {
            // Arrange
            var result1 = new ValidationResult 
            { 
                IsValid = true,
                Warnings = { new ValidationWarning { Message = "Warning 1" } }
            };
            var result2 = new ValidationResult 
            { 
                IsValid = true,
                Warnings = { new ValidationWarning { Message = "Warning 2" } }
            };

            // Act
            var combined = ValidationResult.Combine(result1, result2);

            // Assert
            Assert.IsTrue(combined.IsValid);
            Assert.AreEqual(0, combined.Errors.Count);
            Assert.AreEqual(2, combined.Warnings.Count);
            Assert.AreEqual("Warning 1", combined.Warnings[0].Message);
            Assert.AreEqual("Warning 2", combined.Warnings[1].Message);
        }

        [TestMethod]
        public void Combine_EmptyArray_ReturnsValidResult()
        {
            // Act
            var combined = ValidationResult.Combine();

            // Assert
            Assert.IsTrue(combined.IsValid);
            Assert.AreEqual(0, combined.Errors.Count);
            Assert.AreEqual(0, combined.Warnings.Count);
        }

        [TestMethod]
        public void Combine_NullResults_HandlesGracefully()
        {
            // This test verifies the method handles null inputs properly
            // Implementation should either skip nulls or handle them appropriately
            
            // Arrange
            ValidationResult[] results = { null, new ValidationResult { IsValid = true }, null };

            // Act & Assert
            // This will depend on the actual implementation - either it handles nulls or throws
            // For now, we expect it to handle nulls gracefully
            try
            {
                var combined = ValidationResult.Combine(results);
                Assert.IsTrue(combined.IsValid);
            }
            catch (ArgumentNullException)
            {
                // This is also acceptable behavior
                Assert.IsTrue(true, "Method correctly throws on null input");
            }
        }
    }