using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Tests.ValidationTests;
    [TestClass]
    public class EdgeCasesAndLegacyTests
    {
        [TestMethod]
        public void ValidationResult_Combine_MergesResults()
        {
            // Arrange
            var result1 = new ValidationResult
            {
                IsValid = true,
                Warnings = new List<ValidationWarning> { new ValidationWarning { Message = "Warning 1" } }
            };

            var result2 = new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError> { new ValidationError { Message = "Error 1" } },
                Warnings = new List<ValidationWarning> { new ValidationWarning { Message = "Warning 2" } }
            };

            // Act
            var combined = ValidationResult.Combine(result1, result2);

            // Assert
            Assert.IsFalse(combined.IsValid);
            Assert.AreEqual(1, combined.Errors.Count);
            Assert.AreEqual(2, combined.Warnings.Count);
        }

        [TestMethod]
        public void ValidationResult_Combine_AllValid_ReturnsValid()
        {
            // Arrange
            var result1 = new ValidationResult { IsValid = true };
            var result2 = new ValidationResult { IsValid = true };

            // Act
            var combined = ValidationResult.Combine(result1, result2);

            // Assert
            Assert.IsTrue(combined.IsValid);
        }

        [TestMethod]
        public void ValidationResult_Combine_EmptyArray_ReturnsValid()
        {
            // Act
            var combined = ValidationResult.Combine();

            // Assert
            Assert.IsTrue(combined.IsValid);
            Assert.AreEqual(0, combined.Errors.Count);
            Assert.AreEqual(0, combined.Warnings.Count);
        }

        [TestMethod]
        public void ValidationResult_ThrowIfInvalid_ValidResult_DoesNotThrow()
        {
            // Arrange
            var result = new ValidationResult { IsValid = true };

            // Act & Assert - should not throw
            result.ThrowIfInvalid();
        }

        [TestMethod]
        public void ValidationResult_ThrowIfInvalid_InvalidResult_ThrowsValidationException()
        {
            // Arrange
            var result = new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new ValidationError { Message = "Error 1" },
                    new ValidationError { Message = "Error 2" }
                }
            };

            // Act & Assert
            var exception = Assert.ThrowsException<ValidationException>(() => result.ThrowIfInvalid());
            Assert.IsTrue(exception.Message.Contains("Error 1"));
            Assert.IsTrue(exception.Message.Contains("Error 2"));
        }

        [TestMethod]
        public void ValidationResultCombiner_AddResults_CombinesCorrectly()
        {
            // Arrange
            var combiner = new ValidationResultCombiner();
            var result1 = new ValidationResult
            {
                IsValid = true,
                Warnings = new List<ValidationWarning> { new ValidationWarning { Message = "Warning 1" } }
            };
            var result2 = new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError> { new ValidationError { Message = "Error 1" } }
            };

            // Act
            var combined = combiner.Add(result1).Add(result2).Result;

            // Assert
            Assert.IsFalse(combined.IsValid);
            Assert.AreEqual(1, combined.Errors.Count);
            Assert.AreEqual(1, combined.Warnings.Count);
        }

        [TestMethod]
        public void ValidationResultCombiner_AddBuilder_CombinesBuilderResults()
        {
            // Arrange
            var combiner = new ValidationResultCombiner();
            var builder = new ValidationBuilder<string>("TestField");
            builder.Required();

            // Act
            var combined = combiner.Add(builder, "valid value").Result;

            // Assert
            Assert.IsTrue(combined.IsValid);
        }

        [TestMethod]
        public void ValidationBuilder_CustomValidation_WorksCorrectly()
        {
            // Arrange
            var builder = new ValidationBuilder<int>("TestField");
            builder.Custom((value, context) =>
            {
                var result = new ValidationResult { IsValid = value > 0 };
                if (value <= 0)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Field = context.FieldName,
                        Message = "Value must be positive",
                        Code = "POSITIVE_VALUE_REQUIRED",
                        Value = value
                    });
                }
                return result;
            });

            // Act - valid case
            var validResult = builder.Validate(5);
            
            // Act - invalid case
            var invalidResult = builder.Validate(-1);

            // Assert
            Assert.IsTrue(validResult.IsValid);
            Assert.IsFalse(invalidResult.IsValid);
            Assert.AreEqual(1, invalidResult.Errors.Count);
            Assert.AreEqual("POSITIVE_VALUE_REQUIRED", invalidResult.Errors[0].Code);
        }

        [TestMethod]
        public void ValidationBuilder_OneOfWithEnumerable_WorksCorrectly()
        {
            // Arrange
            var allowedValues = new List<string> { "option1", "option2", "option3" };
            var builder = new ValidationBuilder<string>("TestField");
            builder.OneOf(allowedValues);

            // Act
            var validResult = builder.Validate("option2");
            var invalidResult = builder.Validate("invalid");

            // Assert
            Assert.IsTrue(validResult.IsValid);
            Assert.IsFalse(invalidResult.IsValid);
        }

        [TestMethod]
        public void ValidationError_ToString_FormatsCorrectly()
        {
            // Arrange
            var error = new ValidationError
            {
                Field = "TestField",
                Message = "Test error message"
            };

            // Act
            var result = error.ToString();

            // Assert
            Assert.AreEqual("TestField: Test error message", result);
        }

        [TestMethod]
        public void ValidationError_ToString_NoField_ReturnsMessageOnly()
        {
            // Arrange
            var error = new ValidationError
            {
                Message = "Test error message"
            };

            // Act
            var result = error.ToString();

            // Assert
            Assert.AreEqual("Test error message", result);
        }

        [TestMethod]
        public void ValidationWarning_ToString_FormatsCorrectly()
        {
            // Arrange
            var warning = new ValidationWarning
            {
                Field = "TestField",
                Message = "Test warning message"
            };

            // Act
            var result = warning.ToString();

            // Assert
            Assert.AreEqual("TestField: Test warning message", result);
        }

        [TestMethod]
        public void ValidationContext_Properties_GetSet_WorksCorrectly()
        {
            // Arrange
            var context = new ValidationContext();

            // Act
            context.SetProperty("key1", "value1");
            context.SetProperty("key2", 42);

            // Assert
            Assert.AreEqual("value1", context.GetProperty<string>("key1"));
            Assert.AreEqual(42, context.GetProperty<int>("key2"));
            Assert.AreEqual("default", context.GetProperty("nonexistent", "default"));
        }

        [TestMethod]
        public void PerformanceTest_ManyValidations_PerformsReasonably()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestField");
            builder.Required().AsUri().AsUriScheme();

            // Act
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < 1000; i++)
            {
                var result = builder.Validate("path://scene/GameObject" + i);
                Assert.IsTrue(result.IsValid);
            }
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert - should complete in reasonable time (less than 1 second for 1000 validations)
            Assert.IsTrue(duration.TotalMilliseconds < 1000, $"Performance test took {duration.TotalMilliseconds}ms");
        }

        [TestMethod]
        public void ComplexValidation_MultipleRules_AllRulesApplied()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestField");
            builder.Required()
                   .AsUri()
                   .AsPathUri();

            // Act - valid complex case
            var validResult = builder.Validate("path://scene/GameObject/Child");

            // Act - invalid first rule
            var invalidResult1 = builder.Validate(null);

            // Act - invalid second rule
            var invalidResult2 = builder.Validate("not-a-uri");

            // Act - invalid third rule
            var invalidResult3 = builder.Validate("path://invalid");

            // Assert
            Assert.IsTrue(validResult.IsValid);
            Assert.IsFalse(invalidResult1.IsValid);
            Assert.IsFalse(invalidResult2.IsValid);
            Assert.IsFalse(invalidResult3.IsValid);
        }

        [TestMethod]
        public void ValidationBuilder_NullContext_HandlesGracefully()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestField");
            builder.Required();

            // Act
            var result = builder.Validate("test value", null);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        public class TestObject
        {
            public string Value { get; set; } = "";
            public int Number { get; set; }
        }
    }